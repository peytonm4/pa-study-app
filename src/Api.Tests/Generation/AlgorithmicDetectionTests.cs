using StudyApp.Api.Jobs;
using StudyApp.Api.Models;

namespace StudyApp.Api.Tests.Generation;

public class AlgorithmicDetectionTests
{
    [Fact]
    public void IsAlgorithmic_AlgorithmKeyword_ReturnsTrue()
    {
        var section = new Section
        {
            HeadingText = "Sepsis Algorithm",
            Content = "Standard workup steps."
        };

        var result = SectionGenerationJob.IsAlgorithmic(section);

        Assert.True(result);
    }

    [Fact]
    public void IsAlgorithmic_WorkupKeyword_ReturnsTrue()
    {
        var section = new Section
        {
            HeadingText = "Chest Pain Workup",
            Content = "Initial assessment and management."
        };

        var result = SectionGenerationJob.IsAlgorithmic(section);

        Assert.True(result);
    }

    [Fact]
    public void IsAlgorithmic_PlainSection_ReturnsFalse()
    {
        var section = new Section
        {
            HeadingText = "Vital Signs Overview",
            Content = "Normal values for HR, BP, RR, and temperature."
        };

        var result = SectionGenerationJob.IsAlgorithmic(section);

        Assert.False(result);
    }
}
