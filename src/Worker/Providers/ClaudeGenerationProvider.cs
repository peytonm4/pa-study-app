using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Configuration;
using StudyApp.Api.Providers;

namespace StudyApp.Worker.Providers;

public class ClaudeGenerationProvider(IAnthropicClient client, IConfiguration configuration) : IGenerationProvider
{
    public async Task<string> GenerateAsync(string prompt, IEnumerable<string> sourceChunks)
    {
        var model = configuration["Claude:Model"] ?? "claude-opus-4-5";
        var chunkContext = string.Join("\n\n", sourceChunks);
        var fullPrompt = string.IsNullOrEmpty(chunkContext)
            ? prompt
            : $"Source material:\n{chunkContext}\n\n{prompt}";

        var message = await client.Messages.Create(new MessageCreateParams
        {
            Model = model,
            MaxTokens = 4096,
            Messages =
            [
                new MessageParam
                {
                    Role = Role.User,
                    Content = fullPrompt
                }
            ]
        });

        var textBlock = message.Content
            .Select(b => b.Value)
            .OfType<TextBlock>()
            .FirstOrDefault();

        return textBlock?.Text ?? string.Empty;
    }
}
