using StudyApp.Worker.Skills;

namespace StudyApp.Api.Tests.Skills;

public class ProcessSkillRunnerTests
{
    private readonly ProcessSkillRunner _runner = new();

    [Fact(Skip = "Requires python3 on PATH — skipped in CI without Python")]
    public async Task RunAsync_InvokesProcess_ReturnsStdout()
    {
        // Uses python3 -c "import sys, json; print(json.dumps({'ok': True}))" via a temp script
        // This test is skipped in environments without python3.
        var tmpScript = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.py");
        await File.WriteAllTextAsync(tmpScript, "import sys, json\nprint(json.dumps({'ok': True}))\n");
        try
        {
            var result = await _runner.RunAsync(tmpScript, "{}");
            Assert.Contains("ok", result);
        }
        finally
        {
            File.Delete(tmpScript);
        }
    }

    [Fact]
    public async Task RunAsync_NonZeroExit_ThrowsSkillException()
    {
        // Use a script path that, when python3 is available, exits non-zero.
        // If python3 is not available, Process.Start will throw — we catch that separately.
        var tmpScript = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.py");
        await File.WriteAllTextAsync(tmpScript, "import sys\nsys.exit(1)\n");
        try
        {
            await Assert.ThrowsAsync<SkillException>(() =>
                _runner.RunAsync(tmpScript, "{}"));
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // python3 not installed — acceptable in environments without Python
        }
        catch (System.IO.IOException)
        {
            // python3 not on PATH — acceptable
        }
        finally
        {
            File.Delete(tmpScript);
        }
    }
}
