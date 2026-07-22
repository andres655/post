using SmallBusinessPOS.Domain.Common;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>Categoria configurable para clasificar gastos operativos.</summary>
public class ExpenseCategory : AuditableEntity, ISynchronizableEntity
{
    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Business Business { get; private set; } = null!;

    private ExpenseCategory() { }

    public static ExpenseCategory Create(
        Guid businessId,
        string name,
        string? description = null,
        int sortOrder = 0,
        string? createdBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var category = new ExpenseCategory
        {
            BusinessId = businessId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            SortOrder = sortOrder,
            IsActive = true
        };

        if (createdBy is not null)
            category.SetCreatedBy(createdBy);

        return category;
    }

    public void Update(string name, string? description, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SortOrder = sortOrder;
    }

    public void Enable() => IsActive = true;
    public void Disable() => IsActive = false;
}
