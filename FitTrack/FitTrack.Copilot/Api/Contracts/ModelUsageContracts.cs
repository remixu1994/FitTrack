namespace FitTrack.Copilot.Api.Contracts;

public record ModelUsageOverviewDto(
    string Range,
    DateTime RangeStartUtc,
    DateTime RangeEndUtc,
    int TotalRequests,
    long InputTokens,
    long OutputTokens,
    long CacheReadTokens,
    long CacheWriteTokens,
    long TotalTokens,
    double? TotalCostUsd,
    bool HasUnpricedRequests,
    int ModelCount,
    double RequestsPerMinute,
    double TokensPerMinute);

public record ModelUsageTimeBucketDto(
    DateTime BucketStartUtc,
    string Label,
    int RequestCount,
    long InputTokens,
    long OutputTokens,
    long CacheReadTokens,
    long CacheWriteTokens,
    long TotalTokens,
    double? TotalCostUsd,
    bool HasUnpricedRequests);

public record ModelUsageSeriesPointDto(
    DateTime BucketStartUtc,
    string Label,
    double Value);

public record ModelUsageModelSeriesDto(
    string ModelId,
    string Label,
    IReadOnlyList<ModelUsageSeriesPointDto> Points);

public record ModelUsageChartsDto(
    IReadOnlyList<ModelUsageTimeBucketDto> RequestCostBuckets,
    IReadOnlyList<ModelUsageTimeBucketDto> TokenBuckets,
    IReadOnlyList<ModelUsageModelSeriesDto> ModelCostSeries,
    IReadOnlyList<ModelUsageModelSeriesDto> ModelRequestSeries);

public record ModelRequestLogItemDto(
    string Id,
    DateTime RequestTimeUtc,
    string? ThreadId,
    string? ConversationMessageId,
    string ConnectorId,
    string ConnectorDisplayName,
    string ProviderPreset,
    string Protocol,
    string ModelId,
    string RequestType,
    string Status,
    string? UserAgent,
    string? RequestSummary,
    string? ToolEventsSummary,
    int? DurationMs,
    long? InputTokens,
    long? OutputTokens,
    long? CacheReadTokens,
    long? CacheWriteTokens,
    long? TotalTokens,
    double? CostUsd,
    string? ErrorCode,
    string? ErrorMessage);

public record ModelRequestLogListDto(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<ModelRequestLogItemDto> Items);

public record ModelRequestLogCleanupDto(
    int DeletedCount,
    DateTime CutoffUtc);
