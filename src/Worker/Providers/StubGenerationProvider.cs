namespace StudyApp.Worker.Providers;

public class StubGenerationProvider : IGenerationProvider
{
    public Task<string> GenerateAsync(string prompt, IEnumerable<string> sourceChunks)
        => Task.FromResult($"[Stub] Generated content for: {prompt[..Math.Min(50, prompt.Length)]}");
}
