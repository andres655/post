using SmallBusinessPOS.Domain.Common;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>
/// Categoría de productos. Pertenece a un negocio específico.
/// </summary>
public class Category : AuditableEntity, ISynchronizableEntity
{
    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navegación EF Core
    public Business Business { get; private set; } = null!;

    private readonly List<Product> _products = new();
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    private Category() { }

    public static Category Create(Guid businessId, string name, string? description = null, int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Category
        {
            BusinessId = businessId,
            Name = name.Trim(),
            Description = description?.Trim(),
            SortOrder = sortOrder,
            IsActive = true
        };
    }

    public void Update(string name, string? description, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = description?.Trim();
        SortOrder = sortOrder;
    }

    public void Enable() => IsActive = true;
    public void Disable() => IsActive = false;
}
