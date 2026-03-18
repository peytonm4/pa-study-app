using System.Text.Json;
using StudyApp.Worker.Skills;

namespace StudyApp.Api.Tests.Skills;

public class StubSkillRunnerTests
{
    private readonly StubSkillRunner _runner = new();

    [Fact]
    public async Task RunAsync_ExtractImages_ReturnsDeterministicManifest()
    {
        var result = await _runner.RunAsync("skills/extract_images.py", "{}");

        var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("figures", out _),
            "extract_images path should return JSON with a 'figures' key");
    }

    [Fact]
    public async Task RunAsync_LectureExtractor_ReturnsDeterministicSections()
    {
        var result = await _runner.RunAsync("skills/lecture_extractor.py", "{}");

        var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("sections", out _),
            "other paths should return JSON with a 'sections' key");
    }
}
