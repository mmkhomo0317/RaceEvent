namespace RaceEvent.Services
{
    public record BlobUploadResult(string BlobName, string Url);
    public interface IBlobStorageService
    {
        string? Validate(IFormFile file);
        Task<BlobUploadResult> UploadBannerAsync(IFormFile file, CancellationToken ct = default);
        Task DeleteAsync(string? blobName, CancellationToken ct = default);
    }
}
