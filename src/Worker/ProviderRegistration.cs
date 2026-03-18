using Anthropic;
using Google.GenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudyApp.Worker.Providers;

namespace StudyApp.Worker;

public static class ProviderRegistration
{
    public static IServiceCollection AddProviders(this IServiceCollection services, IConfiguration config)
    {
        var visionProvider = config["VISION_PROVIDER"] ?? "stub";
        if (visionProvider == "gemini")
            services.AddSingleton<IVisionProvider, GeminiVisionProvider>();
        else
            services.AddSingleton<IVisionProvider, StubVisionProvider>();

        var generationProvider = config["GENERATION_PROVIDER"] ?? "stub";
        switch (generationProvider)
        {
            case "claude":
                services.AddSingleton<IGenerationProvider, ClaudeGenerationProvider>();
                break;
            case "gemini":
                services.AddSingleton<IGenerationProvider, GeminiGenerationProvider>();
                break;
            default:
                services.AddSingleton<IGenerationProvider, StubGenerationProvider>();
                break;
        }

        return services;
    }
}
