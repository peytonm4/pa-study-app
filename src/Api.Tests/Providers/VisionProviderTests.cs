using StudyApp.Worker.Providers;

namespace StudyApp.Api.Tests.Providers;

public class VisionProviderTests
{
    [Fact]
    public async Task StubVisionProvider_ReturnsPlaceholderText()
    {
        var provider = new StubVisionProvider();
        var result = await provider.ExtractTextAsync([], "image/png");
        Assert.Equal("[Figure: vision extraction not available in stub mode]", result);
    }
}
