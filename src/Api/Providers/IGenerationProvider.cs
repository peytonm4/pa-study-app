namespace StudyApp.Api.Providers;

public interface IGenerationProvider
{
    Task<string> GenerateAsync(string prompt, IEnumerable<string> sourceChunks);
}
