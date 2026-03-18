using StudyApp.Worker.Providers;

namespace StudyApp.Api.Tests.Providers;

public class GenerationProviderTests
{
    [Fact]
    public async Task StubGenerationProvider_ReturnsDeterministicFallback()
    {
        var provider = new StubGenerationProvider();
        var result = await provider.GenerateAsync("test prompt", []);
        Assert.False(string.IsNullOrEmpty(result));
        Assert.StartsWith("[Stub]", result);
    }

    [Fact]
    public async Task StubGenerationProvider_IsDeterministic()
    {
        var provider = new StubGenerationProvider();
        var result1 = await provider.GenerateAsync("same input", []);
        var result2 = await provider.GenerateAsync("same input", []);
        Assert.Equal(result1, result2);
    }
}
