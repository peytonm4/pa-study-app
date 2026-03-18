namespace StudyApp.Worker.Skills;

public interface ISkillRunner
{
    Task<string> RunAsync(string scriptPath, string inputJson, CancellationToken ct = default);
}
