using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using FitTrack.Copilot.Data;
using FitTrack.Copilot.Service;
using Microsoft.Extensions.AI;

namespace FitTrack.Copilot.Middleware;

public sealed class ModelUsageTrackingChatClient : DelegatingChatClient
{
    private static readonly string[] CacheReadUsageKeys =
    [
        "cache_read_input_tokens",
        "cache_read_tokens",
        "cacheReadInputTokens",
        "cacheReadTokens"
    ];

    private static readonly string[] CacheWriteUsageKeys =
    [
        "cache_creation_input_tokens",
        "cache_write_tokens",
        "cacheWriteInputTokens",
        "cacheWriteTokens"
    ];

    private readonly TenantModelConnector _connector;
    private readonly IModelUsageService _modelUsageService;
    private readonly IModelRequestContextAccessor _contextAccessor;
    private readonly ILogger<ModelUsageTrackingChatClient> _logger;

    public ModelUsageTrackingChatClient(
        IChatClient innerClient,
        TenantModelConnector connector,
        IModelUsageService modelUsageService,
        IModelRequestContextAccessor contextAccessor,
        ILogger<ModelUsageTrackingChatClient> logger)
        : base(innerClient)
    {
        _connector = connector;
        _modelUsageService = modelUsageService;
        _contextAccessor = contextAccessor;
        _logger = logger;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var requestContext = _contextAccessor.Current.Clone();
        var startedAtUtc = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await base.GetResponseAsync(chatMessages, options, cancellationToken);
            stopwatch.Stop();

            await RecordAsync(
                requestContext,
                startedAtUtc,
                stopwatch.Elapsed,
                ExtractUsage(response.Usage),
                null,
                CancellationToken.None);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            await RecordAsync(
                requestContext,
                startedAtUtc,
                stopwatch.Elapsed,
                null,
                ex,
                CancellationToken.None);

            throw;
        }
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => TrackStreamingAsync(chatMessages, options, cancellationToken);

    private async IAsyncEnumerable<ChatResponseUpdate> TrackStreamingAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var requestContext = _contextAccessor.Current.Clone();
        var startedAtUtc = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        UsageSnapshot? usage = null;
        Exception? failure = null;

        await using var enumerator = base.GetStreamingResponseAsync(chatMessages, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        try
        {
            while (true)
            {
                ChatResponseUpdate update;

                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        break;
                    }

                    update = enumerator.Current;
                }
                catch (Exception ex)
                {
                    failure = ex;
                    throw;
                }

                usage = MergeUsage(usage, ExtractUsageFromUpdate(update));
                yield return update;
            }
        }
        finally
        {
            stopwatch.Stop();

            try
            {
                await RecordAsync(
                    requestContext,
                    startedAtUtc,
                    stopwatch.Elapsed,
                    usage,
                    failure,
                    CancellationToken.None);
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "Failed to persist streaming model usage for connector {ConnectorId}.", _connector.Id);
            }
        }
    }

    private async Task RecordAsync(
        ModelRequestContext context,
        DateTime startedAtUtc,
        TimeSpan duration,
        UsageSnapshot? usage,
        Exception? failure,
        CancellationToken cancellationToken)
    {
        var pricing = CalculatePricing(usage);
        var log = new ModelRequestLog
        {
            TenantId = _connector.TenantId,
            UserId = context.UserId ?? "unknown",
            ThreadId = Truncate(context.ThreadId, 64),
            ConversationMessageId = Truncate(context.ConversationMessageId, 64),
            ConnectorId = _connector.Id,
            ConnectorDisplayName = Truncate(_connector.DisplayName, 160) ?? _connector.DisplayName,
            ProviderPreset = Truncate(_connector.ProviderPreset, 80) ?? _connector.ProviderPreset,
            Protocol = _connector.Protocol,
            ModelId = Truncate(_connector.ModelId, 160) ?? _connector.ModelId,
            RequestType = context.RequestType,
            Status = failure is null ? ModelRequestStatus.Succeeded : ModelRequestStatus.Failed,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = startedAtUtc.Add(duration),
            DurationMs = duration.TotalMilliseconds > int.MaxValue ? int.MaxValue : (int)Math.Round(duration.TotalMilliseconds),
            UserAgent = Truncate(context.UserAgent, 512),
            ClientIpHash = Truncate(context.ClientIpHash, 128),
            InputTokens = usage?.InputTokens,
            OutputTokens = usage?.OutputTokens,
            TotalTokens = usage?.TotalTokens,
            CacheReadTokens = usage?.CacheReadTokens,
            CacheWriteTokens = usage?.CacheWriteTokens,
            InputCostUsd = pricing.InputCostUsd,
            OutputCostUsd = pricing.OutputCostUsd,
            CacheReadCostUsd = pricing.CacheReadCostUsd,
            CacheWriteCostUsd = pricing.CacheWriteCostUsd,
            TotalCostUsd = pricing.TotalCostUsd,
            ErrorCode = Truncate(failure?.GetType().Name, 128),
            ErrorMessage = Truncate(failure?.Message, 1024),
            ToolEventsSummary = Truncate(context.ToolEvents is { Count: > 0 } ? string.Join(", ", context.ToolEvents.Distinct(StringComparer.OrdinalIgnoreCase)) : null, 1024),
            RequestSummary = Truncate(context.RequestSummary, 512)
        };

        await _modelUsageService.RecordAsync(log, cancellationToken);
    }

    private PricingSnapshot CalculatePricing(UsageSnapshot? usage)
    {
        if (usage is null)
        {
            return new PricingSnapshot(null, null, null, null, null);
        }

        var inputCost = CalculateCost(usage.InputTokens, _connector.InputTokenPricePer1M);
        var outputCost = CalculateCost(usage.OutputTokens, _connector.OutputTokenPricePer1M);
        var cacheReadCost = CalculateCost(usage.CacheReadTokens, _connector.CacheReadTokenPricePer1M);
        var cacheWriteCost = CalculateCost(usage.CacheWriteTokens, _connector.CacheWriteTokenPricePer1M);

        var missingPrice =
            HasBillableTokensWithoutPrice(usage.InputTokens, _connector.InputTokenPricePer1M) ||
            HasBillableTokensWithoutPrice(usage.OutputTokens, _connector.OutputTokenPricePer1M) ||
            HasBillableTokensWithoutPrice(usage.CacheReadTokens, _connector.CacheReadTokenPricePer1M) ||
            HasBillableTokensWithoutPrice(usage.CacheWriteTokens, _connector.CacheWriteTokenPricePer1M);

        if (missingPrice)
        {
            return new PricingSnapshot(inputCost, outputCost, cacheReadCost, cacheWriteCost, null);
        }

        var totalCost = new[] { inputCost, outputCost, cacheReadCost, cacheWriteCost }
            .Where(value => value.HasValue)
            .Sum(value => value ?? 0d);

        if (!inputCost.HasValue && !outputCost.HasValue && !cacheReadCost.HasValue && !cacheWriteCost.HasValue)
        {
            return new PricingSnapshot(null, null, null, null, null);
        }

        return new PricingSnapshot(inputCost, outputCost, cacheReadCost, cacheWriteCost, totalCost);
    }

    private static bool HasBillableTokensWithoutPrice(long? tokens, double? pricePer1M)
        => tokens.GetValueOrDefault() > 0 && !pricePer1M.HasValue;

    private static double? CalculateCost(long? tokenCount, double? pricePer1M)
    {
        if (!tokenCount.HasValue)
        {
            return null;
        }

        if (tokenCount.Value == 0)
        {
            return 0d;
        }

        if (!pricePer1M.HasValue)
        {
            return null;
        }

        return Math.Round((tokenCount.Value / 1_000_000d) * pricePer1M.Value, 8, MidpointRounding.AwayFromZero);
    }

    private static UsageSnapshot? MergeUsage(UsageSnapshot? current, UsageSnapshot? next)
    {
        if (next is null)
        {
            return current;
        }

        if (current is null)
        {
            return next;
        }

        return new UsageSnapshot(
            next.InputTokens ?? current.InputTokens,
            next.OutputTokens ?? current.OutputTokens,
            next.TotalTokens ?? current.TotalTokens,
            next.CacheReadTokens ?? current.CacheReadTokens,
            next.CacheWriteTokens ?? current.CacheWriteTokens);
    }

    private static UsageSnapshot? ExtractUsageFromUpdate(ChatResponseUpdate update)
    {
        var usageProperty = update.GetType().GetProperty("Usage", BindingFlags.Instance | BindingFlags.Public);
        if (usageProperty?.GetValue(update) is not { } usage)
        {
            return null;
        }

        return ExtractUsage(usage);
    }

    private static UsageSnapshot? ExtractUsage(object? usage)
    {
        if (usage is null)
        {
            return null;
        }

        var inputTokens = ReadLong(usage, "InputTokenCount");
        var outputTokens = ReadLong(usage, "OutputTokenCount");
        var totalTokens = ReadLong(usage, "TotalTokenCount");
        var cacheReadTokens = ReadAdditionalCount(usage, CacheReadUsageKeys);
        var cacheWriteTokens = ReadAdditionalCount(usage, CacheWriteUsageKeys);

        if (!inputTokens.HasValue && !outputTokens.HasValue && !totalTokens.HasValue && !cacheReadTokens.HasValue && !cacheWriteTokens.HasValue)
        {
            return null;
        }

        return new UsageSnapshot(
            inputTokens,
            outputTokens,
            totalTokens ?? SumNullable(inputTokens, outputTokens),
            cacheReadTokens,
            cacheWriteTokens);
    }

    private static long? ReadAdditionalCount(object usage, params string[] keys)
    {
        foreach (var collectionPropertyName in new[] { "AdditionalCounts", "Counts", "AdditionalProperties" })
        {
            var property = usage.GetType().GetProperty(collectionPropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property?.GetValue(usage) is not { } value)
            {
                continue;
            }

            if (TryReadDictionaryValue(value, keys, out var count))
            {
                return count;
            }
        }

        return null;
    }

    private static bool TryReadDictionaryValue(object value, IReadOnlyCollection<string> keys, out long? count)
    {
        if (value is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is string key && keys.Contains(key, StringComparer.OrdinalIgnoreCase))
                {
                    count = ConvertToLong(entry.Value);
                    return count.HasValue;
                }
            }
        }

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is null)
                {
                    continue;
                }

                var itemType = item.GetType();
                var key = itemType.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public)?.GetValue(item) as string;
                if (string.IsNullOrWhiteSpace(key) || !keys.Contains(key, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                count = ConvertToLong(itemType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public)?.GetValue(item));
                return count.HasValue;
            }
        }

        count = null;
        return false;
    }

    private static long? ReadLong(object source, string propertyName)
        => ConvertToLong(source.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(source));

    private static long? ConvertToLong(object? value)
    {
        return value switch
        {
            null => null,
            long longValue => longValue,
            int intValue => intValue,
            short shortValue => shortValue,
            uint uintValue => uintValue,
            ulong ulongValue when ulongValue <= long.MaxValue => (long)ulongValue,
            string stringValue when long.TryParse(stringValue, out var parsed) => parsed,
            _ => null
        };
    }

    private static long? SumNullable(long? left, long? right)
        => left.HasValue || right.HasValue
            ? left.GetValueOrDefault() + right.GetValueOrDefault()
            : null;

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private sealed record UsageSnapshot(
        long? InputTokens,
        long? OutputTokens,
        long? TotalTokens,
        long? CacheReadTokens,
        long? CacheWriteTokens);

    private sealed record PricingSnapshot(
        double? InputCostUsd,
        double? OutputCostUsd,
        double? CacheReadCostUsd,
        double? CacheWriteCostUsd,
        double? TotalCostUsd);
}
