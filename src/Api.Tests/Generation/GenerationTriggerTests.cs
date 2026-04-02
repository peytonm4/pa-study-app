using Xunit;

namespace StudyApp.Api.Tests.Generation;

public class GenerationTriggerTests
{
    [Fact(Skip = "Wave 0 stub")]
    public void PostGenerate_WhenExtractionReady_Returns202AndEnqueuesJob() { }

    [Fact(Skip = "Wave 0 stub")]
    public void PostGenerate_WhenExtractionNotReady_Returns409() { }

    [Fact(Skip = "Wave 0 stub")]
    public void PostGenerate_WhenGenerationQueued_Returns409() { }

    [Fact(Skip = "Wave 0 stub")]
    public void PostGenerate_CreatesGenerationRunInDb() { }
}
