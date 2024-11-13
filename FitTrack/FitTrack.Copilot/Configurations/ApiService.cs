namespace FitTrack.Copilot.Configurations;

/// <summary>
/// AI 服务提供商提供的API 服务
/// </summary>
/// <param name="Name">Api名称</param>
/// <param name="ModelId">模型编码</param>
///  <param name="Models">可用模型列表</param>
public record ApiService(string Name, string ModelId, string[]? ModelIds = null);