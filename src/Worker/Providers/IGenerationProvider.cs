namespace StudyApp.Worker.Providers;

public interface IGenerationProvider
{
    Task<string> GenerateAsync(string prompt, IEnumerable<string> sourceChunks);
}
