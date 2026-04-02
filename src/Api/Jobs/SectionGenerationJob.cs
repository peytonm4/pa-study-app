using System.Text.Json;
using System.Text.Json.Serialization;
using Hangfire;
using StudyApp.Api.Data;
using StudyApp.Api.Models;
using StudyApp.Api.Providers;

namespace StudyApp.Api.Jobs;

[AutomaticRetry(Attempts = 1)]
public class SectionGenerationJob(AppDbContext db, IGenerationProvider generation)
{
    private static readonly string[] AlgorithmicKeywords =
        ["algorithm", "flowchart", "workup", "stepwise", "if/then", "if then"];

    public async Task Execute(Guid sectionId, Guid runId, CancellationToken ct = default)
    {
        var section = await db.Sections.FindAsync([sectionId], ct)
            ?? throw new InvalidOperationException($"Section {sectionId} not found");

        var sourceChunks = new[] { section.Content, section.SourcePageRefsJson ?? "" }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        try
        {
            // 1. Study guide
            var sgJson = await generation.GenerateAsync(StudyGuidePrompt(section), sourceChunks);
            var sgDto = JsonSerializer.Deserialize<StudyGuideDto>(sgJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("Study guide JSON null");
            db.StudyGuides.Add(new StudyGuide
            {
                Id = Guid.NewGuid(),
                SectionId = sectionId,
                DirectAnswer = sgDto.DirectAnswer,
                HighYieldDetailsJson = JsonSerializer.Serialize(sgDto.HighYieldDetails),
                KeyTablesJson = JsonSerializer.Serialize(sgDto.KeyTables),
                MustKnowNumbersJson = JsonSerializer.Serialize(sgDto.MustKnowNumbers),
                SourcesJson = JsonSerializer.Serialize(sgDto.Sources),
                CreatedAt = DateTime.UtcNow
            });

            // 2. Flashcards
            var fcJson = await generation.GenerateAsync(FlashcardPrompt(section), sourceChunks);
            var fcDto = JsonSerializer.Deserialize<FlashcardsResponse>(fcJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("Flashcards JSON null");
            db.Flashcards.AddRange(fcDto.Cards.Select((c, i) => new Flashcard
            {
                Id = Guid.NewGuid(),
                SectionId = sectionId,
                Front = c.Front,
                Back = c.Back,
                CardType = c.Type,
                SourceRefsJson = JsonSerializer.Serialize(c.SourceRefs),
                SortOrder = i,
                CreatedAt = DateTime.UtcNow
            }));

            // 3. Quiz
            var qJson = await generation.GenerateAsync(QuizPrompt(section), sourceChunks);
            var qDto = JsonSerializer.Deserialize<QuizResponse>(qJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("Quiz JSON null");
            db.QuizQuestions.AddRange(qDto.Questions.Select((q, i) => new QuizQuestion
            {
                Id = Guid.NewGuid(),
                SectionId = sectionId,
                QuestionText = q.Question,
                ChoicesJson = JsonSerializer.Serialize(q.Choices),
                CorrectAnswer = q.CorrectAnswer,
                SourceRef = q.SourceRef,
                SortOrder = i,
                CreatedAt = DateTime.UtcNow
            }));

            // 4. Concept map only for algorithmic sections
            if (IsAlgorithmic(section))
            {
                var cmJson = await generation.GenerateAsync(ConceptMapPrompt(section), sourceChunks);
                var cmDto = JsonSerializer.Deserialize<ConceptMapDto>(cmJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new InvalidOperationException("Concept map JSON null");
                db.ConceptMaps.Add(new ConceptMap
                {
                    Id = Guid.NewGuid(),
                    SectionId = sectionId,
                    MermaidSyntax = cmDto.Mermaid,
                    SourceNodeRefsJson = JsonSerializer.Serialize(cmDto.SourceNodeRefs),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            var run = await db.GenerationRuns.FindAsync([runId], ct);
            if (run != null)
            {
                run.Status = GenerationStatus.Failed;
                run.ErrorMessage = $"Section {section.HeadingText}: {ex.Message}";
                run.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            throw;
        }
    }

    internal static bool IsAlgorithmic(Section section)
    {
        var text = $"{section.HeadingText} {section.Content}".ToLowerInvariant();
        return AlgorithmicKeywords.Any(kw => text.Contains(kw));
    }

    private static string StudyGuidePrompt(Section s) =>
        "You are a medical education assistant. Generate a study guide for the section \"" + s.HeadingText + "\".\n\n" +
        "IMPORTANT: Use ONLY information present in the source material.\n\n" +
        "Respond with valid JSON:\n" +
        "{\"direct_answer\":\"...\",\"high_yield_details\":[\"...\"],\"key_tables\":[],\"must_know_numbers\":[],\"sources\":[]}\n\n" +
        "Section content:\n" + s.Content + "\n\n" +
        "Source page references:\n" + (s.SourcePageRefsJson ?? "none");

    private static string FlashcardPrompt(Section s) =>
        "Generate 5-10 flashcards for section \"" + s.HeadingText + "\".\n" +
        "Use ONLY source material. Respond with valid JSON:\n" +
        "{\"cards\":[{\"front\":\"...\",\"back\":\"...\",\"type\":\"qa\",\"source_refs\":[]}]}\n\n" +
        "Section content:\n" + s.Content;

    private static string QuizPrompt(Section s) =>
        "Generate 3-7 multiple-choice questions for section \"" + s.HeadingText + "\".\n" +
        "Use ONLY source material. Respond with valid JSON:\n" +
        "{\"questions\":[{\"question\":\"...\",\"choices\":[\"A\",\"B\",\"C\",\"D\"],\"correct_answer\":\"A\",\"source_ref\":\"\"}]}\n\n" +
        "Section content:\n" + s.Content;

    private static string ConceptMapPrompt(Section s) =>
        "Generate a Mermaid flowchart for section \"" + s.HeadingText + "\".\n" +
        "Use ONLY source material. Respond with valid JSON:\n" +
        "{\"mermaid\":\"flowchart TD\\n  A[Start] --> B{Decision}\",\"source_node_refs\":[]}\n\n" +
        "Section content:\n" + s.Content;
}

internal record StudyGuideDto(
    [property: JsonPropertyName("direct_answer")] string DirectAnswer,
    [property: JsonPropertyName("high_yield_details")] List<string> HighYieldDetails,
    [property: JsonPropertyName("key_tables")] List<KeyTableDto> KeyTables,
    [property: JsonPropertyName("must_know_numbers")] List<string> MustKnowNumbers,
    [property: JsonPropertyName("sources")] List<string> Sources);

internal record KeyTableDto(
    [property: JsonPropertyName("header")] List<string> Header,
    [property: JsonPropertyName("rows")] List<List<string>> Rows);

internal record FlashcardDto(
    [property: JsonPropertyName("front")] string Front,
    [property: JsonPropertyName("back")] string Back,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("source_refs")] List<string> SourceRefs);

internal record FlashcardsResponse(
    [property: JsonPropertyName("cards")] List<FlashcardDto> Cards);

internal record QuizQuestionDto(
    [property: JsonPropertyName("question")] string Question,
    [property: JsonPropertyName("choices")] List<string> Choices,
    [property: JsonPropertyName("correct_answer")] string CorrectAnswer,
    [property: JsonPropertyName("source_ref")] string SourceRef);

internal record QuizResponse(
    [property: JsonPropertyName("questions")] List<QuizQuestionDto> Questions);

internal record ConceptMapDto(
    [property: JsonPropertyName("mermaid")] string Mermaid,
    [property: JsonPropertyName("source_node_refs")] List<string> SourceNodeRefs);
