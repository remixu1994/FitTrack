using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace FitTrack.Copilot.Middleware;

/// <summary>
/// 日志记录中间件（演示 DelegatingChatClient 模式）
/// </summary>
public class LoggingChatClient : DelegatingChatClient
{
    private readonly ILogger<LoggingChatClient> _logger;

    public LoggingChatClient(IChatClient innerClient, ILogger<LoggingChatClient> logger)
        : base(innerClient)
    {
        _logger = logger;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("========== 开始聊天请求 ==========");
        _logger.LogInformation("消息数量: {Count}", chatMessages.Count());
        
        // 记录用户消息
        var userMessage = chatMessages.LastOrDefault(m => m.Role == ChatRole.User);
        if (userMessage?.Text != null)
        {
            _logger.LogInformation("用户输入: {Message}", userMessage.Text);
        }

        // 记录工具配置
        if (options?.Tools?.Count > 0)
        {
            _logger.LogInformation("可用工具数量: {ToolCount}", options.Tools.Count);
            foreach (var tool in options.Tools)
            {
                _logger.LogDebug("工具: {ToolName}", tool.Name);
            }
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 调用内部客户端
            var result = await base.GetResponseAsync(chatMessages, options, cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation("AI响应耗时: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            _logger.LogInformation("完成原因: {FinishReason}", result.FinishReason);

            // 记录响应内容
            if (result.Text != null)
            {
                _logger.LogInformation("AI回复: {Response}", result.Text);
            }

            // 记录工具调用
            var functionCalls = result.Messages
                .SelectMany(m => m.Contents)
                .OfType<FunctionCallContent>()
                .ToList();
            if (functionCalls.Any())
            {
                _logger.LogInformation("工具调用数量: {Count}", functionCalls.Count);
                foreach (var call in functionCalls)
                {
                    _logger.LogInformation("调用工具: {FunctionName}({Arguments})", 
                        call.Name, call.Arguments);
                }
            }

            // 记录 Token 使用情况
            if (result.Usage != null)
            {
                _logger.LogInformation("Token 使用: 输入={InputTokens}, 输出={OutputTokens}, 总计={TotalTokens}",
                    result.Usage.InputTokenCount,
                    result.Usage.OutputTokenCount,
                    result.Usage.TotalTokenCount);
            }

            _logger.LogInformation("========== 聊天请求完成 ==========\n");

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "聊天请求失败，耗时: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            _logger.LogInformation("========== 聊天请求失败 ==========\n");
            throw;
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("========== 开始流式聊天请求 ==========");
        _logger.LogInformation("消息数量: {Count}", chatMessages.Count());

        var userMessage = chatMessages.LastOrDefault(m => m.Role == ChatRole.User);
        if (userMessage?.Text != null)
        {
            _logger.LogInformation("用户输入: {Message}", userMessage.Text);
        }

        var stopwatch = Stopwatch.StartNew();
        var chunkCount = 0;

        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            chunkCount++;
            
            if (update.Text != null && _logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("收到流式数据块 #{ChunkNumber}: {Text}", chunkCount, update.Text);
            }

            yield return update;
        }

        stopwatch.Stop();
        _logger.LogInformation("流式响应完成，共 {ChunkCount} 个数据块，耗时: {ElapsedMs}ms", 
            chunkCount, stopwatch.ElapsedMilliseconds);
        _logger.LogInformation("========== 流式聊天请求完成 ==========\n");
    }
}
