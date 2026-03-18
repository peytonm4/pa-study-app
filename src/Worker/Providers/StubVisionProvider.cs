namespace StudyApp.Worker.Providers;

public class StubVisionProvider : IVisionProvider
{
    public Task<string> ExtractTextAsync(byte[] imageBytes, string mimeType)
        => Task.FromResult("[Figure: vision extraction not available in stub mode]");
}
