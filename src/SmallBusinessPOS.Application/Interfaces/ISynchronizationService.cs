namespace SmallBusinessPOS.Application.Interfaces;

public interface ISynchronizationService
{
    Task<SyncResult> SynchronizeAsync(CancellationToken cancellationToken);
}

public sealed record SyncResult(bool IsSuccess, int Processed, int Failed, string? Message = null);
