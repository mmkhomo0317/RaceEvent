namespace RaceEvent.Models.Interface
{
    public interface IProfileImageService
    {
        Task<string?> SaveAsync(IFormFile file);

        void Delete(string? relativePath);

    }
    public class ProfileImageService : IProfileImageService
    {
        private static readonly string[] Allowed = { ".jpg", ".jpeg", ".png", ".webp" };

        private const long MaxBytes = 2 * 1024 * 1024; // 2 MB

        private readonly IWebHostEnvironment _env;

        public ProfileImageService(IWebHostEnvironment env) => _env = env;

        public async Task<string?> SaveAsync(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!Allowed.Contains(ext) || file.Length == 0 || file.Length > MaxBytes)
                return null;

            var folder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{ext}";   // never trust the client file name
            var fullPath = Path.Combine(folder, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/profiles/{fileName}";
        }

        public void Delete(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;

            var fullPath = Path.Combine(
                _env.WebRootPath,
                relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}
