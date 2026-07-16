using Microsoft.Extensions.Logging;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Infrastructure.Services;

/// <summary>
/// Implementación inicial offline-first.
/// No envía datos a la nube; deja la infraestructura lista para integración futura.
/// </summary>
public sealed class LocalOnlySynchronizationService(ILogger<LocalOnlySynchronizationService> logger) : ISynchronizationService
{
    public Task<SyncResult> SynchronizeAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("SynchronizeAsync invocado en modo local-only. Sin sincronización remota en esta etapa.");
        return Task.FromResult(new SyncResult(
            IsSuccess: true,
            Processed: 0,
            Failed: 0,
            Message: "Sincronización remota no habilitada en esta versión."));
    }
}
