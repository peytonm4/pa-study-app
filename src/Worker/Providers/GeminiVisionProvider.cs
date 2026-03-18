using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Configuration;
using StudyApp.Api.Providers;

namespace StudyApp.Worker.Providers;

public class GeminiVisionProvider(Client client, IConfiguration configuration) : IVisionProvider
{
    public async Task<string> ExtractTextAsync(byte[] imageBytes, string mimeType)
    {
        var model = configuration["Gemini:VisionModel"] ?? "gemini-2.0-flash";
        var response = await client.Models.GenerateContentAsync(
            model: model,
            contents: new Content
            {
                Parts =
                [
                    new Part { InlineData = new() { Data = imageBytes, MimeType = mimeType } },
                    new Part { Text = "Extract all text from this image. Return only the extracted text, no commentary." }
                ]
            });
        return response.Text ?? string.Empty;
    }
}
