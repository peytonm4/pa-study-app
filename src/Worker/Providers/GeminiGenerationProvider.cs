using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Configuration;

namespace StudyApp.Worker.Providers;

public class GeminiGenerationProvider(Client client, IConfiguration configuration) : IGenerationProvider
{
    public async Task<string> GenerateAsync(string prompt, IEnumerable<string> sourceChunks)
    {
        var model = configuration["Gemini:GenerationModel"] ?? "gemini-2.0-flash";
        var chunkContext = string.Join("\n\n", sourceChunks);
        var fullPrompt = string.IsNullOrEmpty(chunkContext)
            ? prompt
            : $"Source material:\n{chunkContext}\n\n{prompt}";

        var response = await client.Models.GenerateContentAsync(
            model: model,
            contents: fullPrompt);

        return response.Text ?? string.Empty;
    }
}
