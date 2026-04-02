using StudyApp.Api.Providers;

namespace StudyApp.Worker.Providers;

public class StubGenerationProvider : IGenerationProvider
{
    public Task<string> GenerateAsync(string prompt, IEnumerable<string> sourceChunks)
    {
        var p = prompt.ToLowerInvariant();
        if (p.Contains("study guide"))
            return Task.FromResult(StubStudyGuideJson());
        if (p.Contains("flashcard"))
            return Task.FromResult(StubFlashcardsJson());
        if (p.Contains("quiz"))
            return Task.FromResult(StubQuizJson());
        if (p.Contains("concept map") || p.Contains("mermaid"))
            return Task.FromResult(StubConceptMapJson());
        return Task.FromResult("[Stub] " + prompt[..Math.Min(50, prompt.Length)]);
    }

    private static string StubStudyGuideJson() => """
        {
          "direct_answer": "Stub direct answer for testing.",
          "high_yield_details": ["Detail 1", "Detail 2"],
          "key_tables": [{"header":["Col A","Col B"],"rows":[["val1","val2"]]}],
          "must_know_numbers": ["42 mmHg", "120/80 mmHg"],
          "sources": ["Slide 1 - stub.pptx"]
        }
        """;

    private static string StubFlashcardsJson() => """
        {
          "cards": [
            {"front":"What is X?","back":"X is Y.","type":"qa","source_refs":["Slide 1"]},
            {"front":"The _____ is responsible for Z.","back":"mitochondria","type":"cloze","source_refs":["Slide 2"]}
          ]
        }
        """;

    private static string StubQuizJson() => """
        {
          "questions": [
            {
              "question":"Which of the following is correct?",
              "choices":["A","B","C","D"],
              "correct_answer":"A",
              "source_ref":"Slide 3"
            }
          ]
        }
        """;

    private static string StubConceptMapJson() => """
        {
          "mermaid": "flowchart TD\n  A[Start] --> B{Decision}\n  B -->|Yes| C[End]\n  B -->|No| D[Loop]",
          "source_node_refs": ["Slide 4"]
        }
        """;
}
