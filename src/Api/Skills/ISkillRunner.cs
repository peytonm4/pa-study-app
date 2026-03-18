namespace StudyApp.Api.Skills;

public interface ISkillRunner
{
    Task<string> RunAsync(string scriptPath, string inputJson, CancellationToken ct = default);
}
