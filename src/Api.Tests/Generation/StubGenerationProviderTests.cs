using System.Text.Json;
using StudyApp.Worker.Providers;

namespace StudyApp.Api.Tests.Generation;

public class StubGenerationProviderTests
{
    private static readonly StubGenerationProvider Provider = new();

    [Fact]
    public async Task GenerateAsync_StudyGuidePrompt_ReturnsValidJson()
    {
        var result = await Provider.GenerateAsync("Generate a study guide for this section", []);

        var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("direct_answer", out var da));
        Assert.Equal(JsonValueKind.String, da.ValueKind);
        Assert.True(doc.RootElement.TryGetProperty("high_yield_details", out var hyd));
        Assert.Equal(JsonValueKind.Array, hyd.ValueKind);
        Assert.True(doc.RootElement.TryGetProperty("key_tables", out var kt));
        Assert.Equal(JsonValueKind.Array, kt.ValueKind);
        Assert.True(doc.RootElement.TryGetProperty("must_know_numbers", out var mkn));
        Assert.Equal(JsonValueKind.Array, mkn.ValueKind);
        Assert.True(doc.RootElement.TryGetProperty("sources", out var src));
        Assert.Equal(JsonValueKind.Array, src.ValueKind);
    }

    [Fact]
    public async Task GenerateAsync_FlashcardPrompt_ReturnsValidJson()
    {
        var result = await Provider.GenerateAsync("Generate flashcard cards for this topic", []);

        var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("cards", out var cards));
        Assert.Equal(JsonValueKind.Array, cards.ValueKind);
        Assert.True(cards.GetArrayLength() > 0);

        var card = cards.EnumerateArray().First();
        Assert.True(card.TryGetProperty("front", out _));
        Assert.True(card.TryGetProperty("back", out _));
        Assert.True(card.TryGetProperty("type", out _));
        Assert.True(card.TryGetProperty("source_refs", out _));
    }

    [Fact]
    public async Task GenerateAsync_QuizPrompt_ReturnsValidJson()
    {
        var result = await Provider.GenerateAsync("Generate quiz questions for this section", []);

        var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("questions", out var questions));
        Assert.Equal(JsonValueKind.Array, questions.ValueKind);
        Assert.True(questions.GetArrayLength() > 0);

        var q = questions.EnumerateArray().First();
        Assert.True(q.TryGetProperty("question", out _));
        Assert.True(q.TryGetProperty("choices", out _));
        Assert.True(q.TryGetProperty("correct_answer", out _));
        Assert.True(q.TryGetProperty("source_ref", out _));
    }

    [Fact]
    public async Task GenerateAsync_ConceptMapPrompt_ReturnsValidJson()
    {
        var result = await Provider.GenerateAsync("Generate a concept map mermaid flowchart for this section", []);

        var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("mermaid", out var mermaid));
        Assert.Equal(JsonValueKind.String, mermaid.ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(mermaid.GetString()));
        Assert.True(doc.RootElement.TryGetProperty("source_node_refs", out var refs));
        Assert.Equal(JsonValueKind.Array, refs.ValueKind);
    }
}
