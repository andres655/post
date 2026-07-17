namespace SmallBusinessPOS.Domain.Enums;

/// <summary>Tipo de cambio local pendiente de sincronizar.</summary>
public enum SyncOperation
{
    Created = 0,
    Updated = 1,
    Deleted = 2,
    Upserted = 3
}
