using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Web.Services;

public sealed class WebRootFileStorageService(IWebHostEnvironment environment) : IFileStorageService
{
    public async Task<string> SaveAsync(
        Stream content,
        string fileName,
        string relativeDirectory,
        IReadOnlySet<string> allowedExtensions,
        long maxBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeDirectory);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            throw new InvalidOperationException("El tipo de archivo no esta permitido.");

        if (content.CanSeek && content.Length > maxBytes)
            throw new InvalidOperationException("El archivo excede el tamano maximo permitido.");

        var cleanDirectory = relativeDirectory.Replace('\\', '/').Trim('/');
        var physicalDirectory = Path.Combine(environment.WebRootPath, cleanDirectory.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(physicalDirectory);

        var storedFileName = $"{Path.GetFileNameWithoutExtension(fileName)}-{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(physicalDirectory, storedFileName);

        await using var output = File.Create(physicalPath);
        await content.CopyToAsync(output, cancellationToken);

        return $"{cleanDirectory}/{storedFileName}";
    }
}
