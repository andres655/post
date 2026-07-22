using SmallBusinessPOS.Domain.Common;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>Opcion configurable para mostrar tipos de producto desde base de datos.</summary>
public class ProductTypeOption : AuditableEntity, ISynchronizableEntity
{
    public Guid BusinessId { get; private set; }
    public ProductType Value { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Business Business { get; private set; } = null!;

    private ProductTypeOption() { }

    public static ProductTypeOption Create(
        Guid businessId,
        ProductType value,
        string name,
        string? description = null,
        int sortOrder = 0,
        string? createdBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var option = new ProductTypeOption
        {
            BusinessId = businessId,
            Value = value,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            SortOrder = sortOrder,
            IsActive = true
        };

        if (createdBy is not null)
            option.SetCreatedBy(createdBy);

        return option;
    }

    public void Update(string name, string? description, int sortOrder, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SortOrder = sortOrder;
        IsActive = isActive;
    }
}
