namespace SmallBusinessPOS.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(
        Stream content,
        string fileName,
        string relativeDirectory,
        IReadOnlySet<string> allowedExtensions,
        long maxBytes,
        CancellationToken cancellationToken = default);
}
