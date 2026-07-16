namespace SmallBusinessPOS.Domain.Enums;

/// <summary>Estado de sincronización con servidor remoto.</summary>
public enum SyncStatus
{
    /// <summary>Pendiente de sincronizar.</summary>
    Pending = 0,

    /// <summary>Sincronizado exitosamente.</summary>
    Synced = 1,

    /// <summary>Modificado localmente después de sincronizar.</summary>
    Modified = 2,

    /// <summary>Error en el último intento de sincronización.</summary>
    Failed = 3
}
