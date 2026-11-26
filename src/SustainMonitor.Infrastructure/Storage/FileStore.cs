namespace SustainMonitor.Infrastructure.Storage;

/// <summary>Where evidence files live. Local disk now; swap for Azure Blob later without touching endpoints.</summary>
public interface IFileStore
{
    /// <returns>The storage path to save in <c>Evidence.StoragePath</c>.</returns>
    Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken ct = default);
    Stream OpenRead(string storagePath);
}

public sealed class LocalFileStore(string rootFolder) : IFileStore
{
    public static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".docx", ".xlsx", ".csv"];
    public const long MaxBytes = 20 * 1024 * 1024;

    public async Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidDataException($"File type {ext} is not allowed. Use PDF, images, Word, Excel or CSV.");

        var relative = Path.Combine(DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"), $"{Guid.NewGuid():N}{ext}");
        var full = Path.Combine(rootFolder, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var file = File.Create(full);
        await content.CopyToAsync(file, ct);
        return relative.Replace('\\', '/');
    }

    public Stream OpenRead(string storagePath)
    {
        var full = Path.GetFullPath(Path.Combine(rootFolder, storagePath));
        var root = Path.GetFullPath(rootFolder);
        if (!full.StartsWith(root, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Invalid storage path.");
        return File.OpenRead(full);
    }
}
