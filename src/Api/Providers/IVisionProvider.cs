namespace StudyApp.Api.Providers;

public interface IVisionProvider
{
    Task<string> ExtractTextAsync(byte[] imageBytes, string mimeType);
}
