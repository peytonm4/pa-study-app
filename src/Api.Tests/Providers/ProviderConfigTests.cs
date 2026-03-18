using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudyApp.Worker;
using StudyApp.Worker.Providers;

namespace StudyApp.Api.Tests.Providers;

public class ProviderConfigTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

    [Fact]
    public void VisionProviderEnvVar_Stub_RegistersStubVisionProvider()
    {
        var config = BuildConfig(new() { ["VISION_PROVIDER"] = "stub" });
        var services = new ServiceCollection().AddProviders(config);
        var provider = services.BuildServiceProvider().GetRequiredService<IVisionProvider>();

        Assert.IsType<StubVisionProvider>(provider);
    }

    [Fact]
    public void GenerationProviderEnvVar_Stub_RegistersStubGenerationProvider()
    {
        var config = BuildConfig(new() { ["GENERATION_PROVIDER"] = "stub" });
        var services = new ServiceCollection().AddProviders(config);
        var provider = services.BuildServiceProvider().GetRequiredService<IGenerationProvider>();

        Assert.IsType<StubGenerationProvider>(provider);
    }

    [Fact]
    public void GenerationProviderEnvVar_Missing_DefaultsToStub()
    {
        var config = BuildConfig(new());
        var services = new ServiceCollection().AddProviders(config);
        var provider = services.BuildServiceProvider().GetRequiredService<IGenerationProvider>();

        Assert.IsType<StubGenerationProvider>(provider);
    }
}
