using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudyApp.Worker;
using StudyApp.Worker.Skills;

namespace StudyApp.Api.Tests.Skills;

public class SkillProviderConfigTests
{
    private static IServiceProvider BuildProvider(string? pythonProvider)
    {
        var configData = new Dictionary<string, string?>();
        if (pythonProvider is not null)
            configData["PYTHON_PROVIDER"] = pythonProvider;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddProviders(config);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void WhenPythonProviderStub_UsesStubSkillRunner()
    {
        var provider = BuildProvider("stub");
        var runner = provider.GetRequiredService<ISkillRunner>();
        Assert.IsType<StubSkillRunner>(runner);
    }

    [Fact]
    public void WhenPythonProviderUnset_UsesStubSkillRunner()
    {
        var provider = BuildProvider(null);
        var runner = provider.GetRequiredService<ISkillRunner>();
        Assert.IsType<StubSkillRunner>(runner);
    }

    [Fact]
    public void WhenPythonProviderReal_UsesProcessSkillRunner()
    {
        var provider = BuildProvider("real");
        var runner = provider.GetRequiredService<ISkillRunner>();
        Assert.IsType<ProcessSkillRunner>(runner);
    }
}
