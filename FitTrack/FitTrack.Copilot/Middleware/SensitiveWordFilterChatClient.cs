using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace FitTrack.Copilot.Middleware;

/// <summary>
/// 敏感词过滤中间件（演示内容安全）
/// </summary>
public class SensitiveWordFilterChatClient : DelegatingChatClient
{
    private readonly ILogger<SensitiveWordFilterChatClient> _logger;
    private readonly HashSet<string> _sensitiveWords;

    public SensitiveWordFilterChatClient(IChatClient innerClient, ILogger<SensitiveWordFilterChatClient> logger)
        : base(innerClient)
    {
        _logger = logger;
        
        // 初始化敏感词库（实际应用应该从配置文件或数据库加载）
        _sensitiveWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "垃圾", "废物", "骗子", "投诉", "举报"
            // 这里只是示例，实际应该有更完整的敏感词库
        };
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // 检查用户输入是否包含敏感词
        var lastUserMessage = chatMessages.LastOrDefault(m => m.Role == ChatRole.User);
        if (lastUserMessage?.Text != null)
        {
            var detectedWords = DetectSensitiveWords(lastUserMessage.Text);
            if (detectedWords.Any())
            {
                _logger.LogWarning("🚨 检测到敏感词: {Words}", string.Join(", ", detectedWords));
                
                // 记录敏感词但不阻止请求（实际应用中可以根据策略决定是否阻止）
                // 可以选择：1) 直接阻止  2) 替换敏感词  3) 仅记录日志
                
                // 这里我们选择仅记录，不修改消息
            }
        }

        // 调用内部客户端
        var result = await base.GetResponseAsync(chatMessages, options, cancellationToken);

        // 检查 AI 响应是否包含敏感词
        if (result.Text != null)
        {
            var detectedWords = DetectSensitiveWords(result.Text);
            if (detectedWords.Any())
            {
                _logger.LogWarning("🚨 AI响应包含敏感词: {Words}", string.Join(", ", detectedWords));
                
                // 可以选择过滤或替换
                // 这里仅做演示，实际应用需要更复杂的策略
            }
        }

        return result;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 检查用户输入
        var lastUserMessage = chatMessages.LastOrDefault(m => m.Role == ChatRole.User);
        if (lastUserMessage?.Text != null)
        {
            var detectedWords = DetectSensitiveWords(lastUserMessage.Text);
            if (detectedWords.Any())
            {
                _logger.LogWarning("🚨 检测到敏感词: {Words}", string.Join(", ", detectedWords));
            }
        }

        // 流式处理
        var responseTextBuilder = new System.Text.StringBuilder();

        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            if (update.Text != null)
            {
                responseTextBuilder.Append(update.Text);
            }

            yield return update;
        }

        // 检查完整响应
        var fullResponse = responseTextBuilder.ToString();
        if (!string.IsNullOrEmpty(fullResponse))
        {
            var detectedWords = DetectSensitiveWords(fullResponse);
            if (detectedWords.Any())
            {
                _logger.LogWarning("🚨 流式响应包含敏感词: {Words}", string.Join(", ", detectedWords));
            }
        }
    }

    /// <summary>
    /// 检测文本中的敏感词
    /// </summary>
    private List<string> DetectSensitiveWords(string text)
    {
        var detected = new List<string>();

        foreach (var word in _sensitiveWords)
        {
            if (text.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                detected.Add(word);
            }
        }

        return detected;
    }

    /// <summary>
    /// 替换敏感词（如果需要）
    /// </summary>
    private string ReplaceSensitiveWords(string text, char replacement = '*')
    {
        foreach (var word in _sensitiveWords)
        {
            if (text.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                var replacementText = new string(replacement, word.Length);
                text = System.Text.RegularExpressions.Regex.Replace(
                    text, 
                    word, 
                    replacementText, 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
        }

        return text;
    }
}
