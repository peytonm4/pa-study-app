namespace StudyApp.Worker.Skills;

public class StubSkillRunner : ISkillRunner
{
    public Task<string> RunAsync(string scriptPath, string inputJson, CancellationToken ct = default)
    {
        if (scriptPath.Contains("extract_images"))
            return Task.FromResult(StubFigureManifest);
        return Task.FromResult(StubLectureSections);
    }

    private const string StubFigureManifest = """
        {"figures":[{"id":"stub-fig-1","s3_key":"stub/fig1.png","page":1,"has_caption":true,"caption_keywords":["Figure"]}]}
        """;

    private const string StubLectureSections = """
        {"sections":[{"level":1,"heading":"Stub Topic","content":"Stub content for stub mode.","pages":[1],"figures":[]}]}
        """;
}
