using System.Diagnostics;

namespace StudyApp.Worker.Skills;

public class ProcessSkillRunner : ISkillRunner
{
    public async Task<string> RunAsync(string scriptPath, string inputJson, CancellationToken ct = default)
    {
        var tmpInput = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        await File.WriteAllTextAsync(tmpInput, inputJson, ct);
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = $"\"{scriptPath}\" \"{tmpInput}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(psi)!;
            var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            if (process.ExitCode != 0)
                throw new SkillException($"Script exited {process.ExitCode}: {stderr}");
            return stdout;
        }
        finally
        {
            File.Delete(tmpInput);
        }
    }
}
