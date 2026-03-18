namespace StudyApp.Api.Services;

public interface IStorageService
{
    Task<string> UploadAsync(Stream stream, string key, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}
