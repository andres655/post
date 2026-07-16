namespace SmallBusinessPOS.Domain.Common;

/// <summary>
/// Clase base para todas las entidades del dominio.
/// Utiliza Guid versión 7 para IDs ordenables por tiempo.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; private set; }

    protected Entity()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>Constructor para EF Core y reconstrucción desde persistencia.</summary>
    protected Entity(Guid id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}
