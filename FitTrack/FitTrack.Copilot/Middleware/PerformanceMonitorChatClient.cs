using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace FitTrack.Copilot.Middleware;

/// <summary>
/// 性能监控中间件（演示性能追踪和统计）
/// </summary>
public class PerformanceMonitorChatClient : DelegatingChatClient
{
    private readonly ILogger<PerformanceMonitorChatClient> _logger;
    private int _requestCount = 0;
    private long _totalResponseTime = 0;
    private int _totalInputTokens = 0;
    private int _totalOutputTokens = 0;

    public PerformanceMonitorChatClient(IChatClient innerClient, ILogger<PerformanceMonitorChatClient> logger)
        : base(innerClient)
    {
        _logger = logger;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var requestId = Interlocked.Increment(ref _requestCount);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await base.GetResponseAsync(chatMessages, options, cancellationToken);
            stopwatch.Stop();

            // 更新统计数据
            Interlocked.Add(ref _totalResponseTime, stopwatch.ElapsedMilliseconds);
            
            if (result.Usage != null)
            {
                Interlocked.Add(ref _totalInputTokens, (int)result.Usage.InputTokenCount.GetValueOrDefault());
                Interlocked.Add(ref _totalOutputTokens, (int)result.Usage.OutputTokenCount.GetValueOrDefault());
            }

            // 计算平均值
            var avgResponseTime = _totalResponseTime / _requestCount;

            // 性能日志
            _logger.LogInformation(
                "📊 性能统计 [请求 #{RequestId}] - " +
                "响应时间: {ElapsedMs}ms | " +
                "平均响应时间: {AvgMs}ms | " +
                "总请求数: {TotalRequests} | " +
                "累计输入Token: {InputTokens} | " +
                "累计输出Token: {OutputTokens}",
                requestId,
                stopwatch.ElapsedMilliseconds,
                avgResponseTime,
                _requestCount,
                _totalInputTokens,
                _totalOutputTokens);

            // 性能告警
            if (stopwatch.ElapsedMilliseconds > 5000)
            {
                _logger.LogWarning("⚠️  响应时间过长: {ElapsedMs}ms (请求 #{RequestId})", 
                    stopwatch.ElapsedMilliseconds, requestId);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "❌ 请求失败 [请求 #{RequestId}]，耗时: {ElapsedMs}ms", 
                requestId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestId = Interlocked.Increment(ref _requestCount);
        var stopwatch = Stopwatch.StartNew();
        var firstChunkTime = 0L;
        var chunkCount = 0;

        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            if (chunkCount == 0)
            {
                firstChunkTime = stopwatch.ElapsedMilliseconds;
                _logger.LogInformation("⚡ 首个数据块到达时间: {Ms}ms [请求 #{RequestId}]", 
                    firstChunkTime, requestId);
            }

            chunkCount++;
            yield return update;
        }

        stopwatch.Stop();
        Interlocked.Add(ref _totalResponseTime, stopwatch.ElapsedMilliseconds);

        _logger.LogInformation(
            "📊 流式性能统计 [请求 #{RequestId}] - " +
            "首包时间: {FirstChunkMs}ms | " +
            "总耗时: {TotalMs}ms | " +
            "数据块数: {ChunkCount}",
            requestId,
            firstChunkTime,
            stopwatch.ElapsedMilliseconds,
            chunkCount);
    }

    /// <summary>
    /// 获取性能统计摘要
    /// </summary>
    public string GetPerformanceSummary()
    {
        if (_requestCount == 0)
        {
            return "暂无性能数据";
        }

        var avgResponseTime = _totalResponseTime / _requestCount;
        
        return $"性能统计摘要：\n" +
               $"- 总请求数: {_requestCount}\n" +
               $"- 平均响应时间: {avgResponseTime}ms\n" +
               $"- 累计输入Token: {_totalInputTokens}\n" +
               $"- 累计输出Token: {_totalOutputTokens}\n" +
               $"- 总Token: {_totalInputTokens + _totalOutputTokens}";
    }
}
