namespace SmallBusinessPOS.Domain.Common;

/// <summary>
/// Marca entidades que pertenecen a un negocio y pueden participar en sincronizacion futura.
/// </summary>
public interface ISynchronizableEntity
{
    Guid BusinessId { get; }
}
