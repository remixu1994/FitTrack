using FitTrack.Copilot.Api.Contracts;
using FitTrack.Copilot.Data;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Copilot.Service;

public interface IModelUsageService
{
    Task RecordAsync(ModelRequestLog log, CancellationToken ct = default);
    Task<ModelUsageOverviewDto> GetOverviewAsync(string tenantId, string range, CancellationToken ct = default);
    Task<ModelUsageChartsDto> GetChartsAsync(string tenantId, string range, CancellationToken ct = default);
    Task<ModelRequestLogListDto> GetLogsAsync(string tenantId, string range, string? connectorId, string? modelSearch, string? status, string? requestType, int page, int pageSize, CancellationToken ct = default);
    Task<ModelRequestLogCleanupDto> CleanupAsync(string? tenantId = null, CancellationToken ct = default);
}

public sealed class ModelUsageService : IModelUsageService
{
    private const int RetentionDays = 90;
    private readonly ApplicationDbContext _dbContext;

    public ModelUsageService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RecordAsync(ModelRequestLog log, CancellationToken ct = default)
    {
        _dbContext.ModelRequestLogs.Add(log);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<ModelUsageOverviewDto> GetOverviewAsync(string tenantId, string range, CancellationToken ct = default)
    {
        var window = ResolveRange(range);
        await EnsureTenantExistsAsync(tenantId, ct);

        var logs = await BaseTenantQuery(tenantId, window.StartUtc)
            .Select(log => new
            {
                log.ModelId,
                log.InputTokens,
                log.OutputTokens,
                log.TotalTokens,
                log.CacheReadTokens,
                log.CacheWriteTokens,
                log.TotalCostUsd
            })
            .ToListAsync(ct);

        var totalRequests = logs.Count;
        var totalMinutes = Math.Max(1d, (window.EndUtc - window.StartUtc).TotalMinutes);
        var pricedRequests = logs.Where(item => item.TotalCostUsd.HasValue).ToList();

        return new ModelUsageOverviewDto(
            window.RangeKey,
            window.StartUtc,
            window.EndUtc,
            totalRequests,
            logs.Sum(item => item.InputTokens ?? 0),
            logs.Sum(item => item.OutputTokens ?? 0),
            logs.Sum(item => item.CacheReadTokens ?? 0),
            logs.Sum(item => item.CacheWriteTokens ?? 0),
            logs.Sum(item => item.TotalTokens ?? 0),
            pricedRequests.Count == 0 ? null : pricedRequests.Sum(item => item.TotalCostUsd ?? 0),
            totalRequests > pricedRequests.Count,
            logs.Select(item => item.ModelId).Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            Math.Round(totalRequests / totalMinutes, 2),
            Math.Round(logs.Sum(item => item.TotalTokens ?? 0) / totalMinutes, 2));
    }

    public async Task<ModelUsageChartsDto> GetChartsAsync(string tenantId, string range, CancellationToken ct = default)
    {
        var window = ResolveRange(range);
        await EnsureTenantExistsAsync(tenantId, ct);

        var logs = await BaseTenantQuery(tenantId, window.StartUtc)
            .Select(log => new ChartLogProjection(
                log.StartedAtUtc,
                log.ModelId,
                log.ConnectorDisplayName,
                log.RequestType,
                log.InputTokens,
                log.OutputTokens,
                log.TotalTokens,
                log.CacheReadTokens,
                log.CacheWriteTokens,
                log.TotalCostUsd))
            .ToListAsync(ct);

        var buckets = BuildBuckets(window);
        var requestCostBuckets = buckets
            .Select(bucket => BuildTimeBucket(bucket, logs.Where(log => bucket.Contains(log.StartedAtUtc)).ToList()))
            .ToList();

        var tokenBuckets = requestCostBuckets;

        var topModels = logs
            .GroupBy(log => log.ModelId)
            .Select(group => new
            {
                ModelId = group.Key,
                Label = $"{group.Key} · {group.First().ConnectorDisplayName}",
                Requests = group.Count(),
                Cost = group.Where(item => item.TotalCostUsd.HasValue).Sum(item => item.TotalCostUsd ?? 0)
            })
            .OrderByDescending(item => item.Requests)
            .ThenByDescending(item => item.Cost)
            .Take(6)
            .ToList();

        var modelCostSeries = topModels
            .Select(model => new ModelUsageModelSeriesDto(
                model.ModelId,
                model.Label,
                buckets.Select(bucket => new ModelUsageSeriesPointDto(
                    bucket.StartUtc,
                    bucket.Label,
                    logs.Where(log => log.ModelId == model.ModelId && bucket.Contains(log.StartedAtUtc))
                        .Sum(log => log.TotalCostUsd ?? 0)))
                    .ToList()))
            .ToList();

        var modelRequestSeries = topModels
            .Select(model => new ModelUsageModelSeriesDto(
                model.ModelId,
                model.Label,
                buckets.Select(bucket => new ModelUsageSeriesPointDto(
                    bucket.StartUtc,
                    bucket.Label,
                    logs.Count(log => log.ModelId == model.ModelId && bucket.Contains(log.StartedAtUtc))))
                    .ToList()))
            .ToList();

        return new ModelUsageChartsDto(requestCostBuckets, tokenBuckets, modelCostSeries, modelRequestSeries);
    }

    public async Task<ModelRequestLogListDto> GetLogsAsync(string tenantId, string range, string? connectorId, string? modelSearch, string? status, string? requestType, int page, int pageSize, CancellationToken ct = default)
    {
        var window = ResolveRange(range);
        await EnsureTenantExistsAsync(tenantId, ct);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = BaseTenantQuery(tenantId, window.StartUtc);

        if (!string.IsNullOrWhiteSpace(connectorId))
        {
            query = query.Where(log => log.ConnectorId == connectorId.Trim());
        }

        if (!string.IsNullOrWhiteSpace(modelSearch))
        {
            var search = modelSearch.Trim();
            query = query.Where(log => log.ModelId.Contains(search) || log.ConnectorDisplayName.Contains(search));
        }

        if (Enum.TryParse<ModelRequestStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(log => log.Status == parsedStatus);
        }

        if (Enum.TryParse<ModelRequestType>(requestType, true, out var parsedRequestType))
        {
            query = query.Where(log => log.RequestType == parsedRequestType);
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(log => log.StartedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new ModelRequestLogItemDto(
                log.Id,
                log.StartedAtUtc,
                log.ThreadId,
                log.ConversationMessageId,
                log.ConnectorId,
                log.ConnectorDisplayName,
                log.ProviderPreset,
                log.Protocol.ToString(),
                log.ModelId,
                log.RequestType.ToString(),
                log.Status.ToString(),
                log.UserAgent,
                log.RequestSummary,
                log.ToolEventsSummary,
                log.DurationMs,
                log.InputTokens,
                log.OutputTokens,
                log.CacheReadTokens,
                log.CacheWriteTokens,
                log.TotalTokens,
                log.TotalCostUsd,
                log.ErrorCode,
                log.ErrorMessage))
            .ToListAsync(ct);

        return new ModelRequestLogListDto(page, pageSize, totalCount, items);
    }

    public async Task<ModelRequestLogCleanupDto> CleanupAsync(string? tenantId = null, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            await EnsureTenantExistsAsync(tenantId, ct);
        }

        var cutoffUtc = DateTime.UtcNow.AddDays(-RetentionDays);
        var query = _dbContext.ModelRequestLogs.Where(log => log.StartedAtUtc < cutoffUtc);

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            query = query.Where(log => log.TenantId == tenantId);
        }

        var deletedCount = await query.ExecuteDeleteAsync(ct);
        return new ModelRequestLogCleanupDto(deletedCount, cutoffUtc);
    }

    private IQueryable<ModelRequestLog> BaseTenantQuery(string tenantId, DateTime startUtc)
        => _dbContext.ModelRequestLogs
            .AsNoTracking()
            .Where(log => log.TenantId == tenantId && log.StartedAtUtc >= startUtc);

    private async Task EnsureTenantExistsAsync(string tenantId, CancellationToken ct)
    {
        if (!await _dbContext.Tenants.AnyAsync(tenant => tenant.Id == tenantId, ct))
        {
            throw new TenantModelConnectorValidationException("TENANT_NOT_FOUND", $"Tenant '{tenantId}' was not found.", System.Net.HttpStatusCode.NotFound);
        }
    }

    private static ModelUsageTimeBucketDto BuildTimeBucket(TimeBucket bucket, IReadOnlyList<ChartLogProjection> items)
    {
        var pricedItems = items.Where(item => item.TotalCostUsd.HasValue).ToList();
        return new ModelUsageTimeBucketDto(
            bucket.StartUtc,
            bucket.Label,
            items.Count,
            items.Sum(item => item.InputTokens ?? 0),
            items.Sum(item => item.OutputTokens ?? 0),
            items.Sum(item => item.CacheReadTokens ?? 0),
            items.Sum(item => item.CacheWriteTokens ?? 0),
            items.Sum(item => item.TotalTokens ?? 0),
            pricedItems.Count == 0 ? null : pricedItems.Sum(item => item.TotalCostUsd ?? 0),
            items.Count > pricedItems.Count);
    }

    private static IReadOnlyList<TimeBucket> BuildBuckets(ModelUsageRangeWindow window)
    {
        var buckets = new List<TimeBucket>();
        for (var cursor = window.StartUtc; cursor < window.EndUtc; cursor = cursor.AddHours(1))
        {
            var end = cursor.AddHours(1);
            buckets.Add(new TimeBucket(cursor, end, cursor.ToLocalTime().ToString("MM-dd HH:mm")));
        }

        return buckets;
    }

    private static ModelUsageRangeWindow ResolveRange(string? range)
    {
        var endUtc = DateTime.UtcNow;
        return (range ?? "24h").Trim().ToLowerInvariant() switch
        {
            "7d" => new ModelUsageRangeWindow("7d", endUtc.AddDays(-7), endUtc),
            "30d" => new ModelUsageRangeWindow("30d", endUtc.AddDays(-30), endUtc),
            _ => new ModelUsageRangeWindow("24h", endUtc.AddHours(-24), endUtc)
        };
    }

    private sealed record ModelUsageRangeWindow(string RangeKey, DateTime StartUtc, DateTime EndUtc);

    private sealed record TimeBucket(DateTime StartUtc, DateTime EndUtc, string Label)
    {
        public bool Contains(DateTime timestamp) => timestamp >= StartUtc && timestamp < EndUtc;
    }

    private sealed record ChartLogProjection(
        DateTime StartedAtUtc,
        string ModelId,
        string ConnectorDisplayName,
        ModelRequestType RequestType,
        long? InputTokens,
        long? OutputTokens,
        long? TotalTokens,
        long? CacheReadTokens,
        long? CacheWriteTokens,
        double? TotalCostUsd);
}
