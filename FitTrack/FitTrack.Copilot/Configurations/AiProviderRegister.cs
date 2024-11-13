using Microsoft.SemanticKernel;
using NLog.Extensions.Logging;

namespace FitTrack.Copilot.Configurations;

public abstract class AiProviderRegister
{
    public abstract AiProviderType AiProviderType { get; }

    public virtual void Register(IServiceCollection services, AiProvider aiProvider, string defaultProvider)
    {
        //将默认的Provider注册为默认的Kernel
        if (aiProvider.Code == defaultProvider)
        {
            services.AddTransient<Kernel>(sp => BuildKernel(sp, aiProvider));
        }
        // 为指定AiProvider 注册为Keyed Kernel服务
        services.AddKeyedTransient<Kernel>(aiProvider.Code, (sp, key) => BuildKernel(sp, aiProvider));
    }

    public virtual Kernel BuildKernel(IServiceProvider serviceProvider, AiProvider aiProvider)
    {
        // 创建Kernel构建器
        var builder = Kernel.CreateBuilder();

        // 注册日志服务
        builder.Services.AddNLog();

        RegisterChatCompletionService(builder, serviceProvider, aiProvider);
        RegisterEmbeddingService(builder, serviceProvider, aiProvider);
        
        // Register other services if needed

        return builder.Build();
    }

    protected abstract void RegisterChatCompletionService(IKernelBuilder builder, IServiceProvider provider, AiProvider aiProvider);

    protected abstract void RegisterEmbeddingService(IKernelBuilder builder, IServiceProvider provider, AiProvider aiProvider);
}