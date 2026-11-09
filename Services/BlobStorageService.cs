using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;

namespace RaceEvent.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private const long MaxBytes = 2 * 1024 * 1024; // 2MB

        private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };
        private readonly BlobContainerClient _container;

        public BlobStorageService(IConfiguration config)
        {
            var connectionString = config["AzureStorage:ConnectionString"];
            var containerName = config["AzureStorage:ContainerName"] ?? "event-banners";

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");

            try
            {
                _container = new BlobContainerClient(connectionString.Trim(), containerName);
            }
            catch (FormatException)
            {
                var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                             .Select(p => p.Split('=')[0].Trim());
                throw new InvalidOperationException($"Connection string is invalid.legnth={connectionString.Length}; " +
                                                    $"starts with '{connectionString[..Math.Min(25, connectionString.Length)]}';" +
                                                    $"keys found: {String.Join(",", parts)}");
            }
        }

        public string? Validate(IFormFile file)
        {
            if (file.Length == 0) return "The selected file is empty.";
            if (file.Length > MaxBytes) return "The banner must be 2 MB or smaller.";

            var ext = Path.GetExtension(file.FileName);

            if (!AllowedTypes.TryGetValue(ext, out var expectedType) ||
                 !string.Equals(file.ContentType, expectedType, StringComparison.OrdinalIgnoreCase))
                return "Only JPG, PNG, WebP images are allowed.";

            //check the files real signature 
            using var stream = file.OpenReadStream();
            Span<byte> h = stackalloc byte[12];

            var read = stream.Read(h);

            var valid = expectedType switch
            {
                "image/jpeg" => read >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF,
                "image/png" => read >= 8 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47,
                "image/webp" => read >= 12 && h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46
                                && h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50,

                _ => false
            };

            return valid ? null : "The file content does not match the expected image type.";

        }

        public async Task<BlobUploadResult> UploadBannerAsync(IFormFile file, CancellationToken ct = default)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var blobName = $"banners/{Guid.NewGuid():N}{ext}"; //unique name: never trust the clients name
            var blob = _container.GetBlobClient(blobName);

            await using var stream = file.OpenReadStream();

            await blob.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = AllowedTypes[ext],
                    CacheControl = "public, max-age=31536000"  // safe: every upload gets a new name
                }
            }, ct);

            return new BlobUploadResult(blobName, blob.Uri.ToString());
        }

        public async Task DeleteAsync(string? blobName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blobName)) return;
            await _container.DeleteBlobIfExistsAsync(blobName, cancellationToken: ct);
        }
    }
}
