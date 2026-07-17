using SmallBusinessPOS.Domain.Common;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>
/// Producto o servicio del negocio.
/// Soporta productos estándar, preparados, combos, servicios, ingredientes y empaques.
/// </summary>
public class Product : AuditableEntity, ISynchronizableEntity
{
    public Guid BusinessId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string? Barcode { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProductType ProductType { get; private set; }
    public UnitOfMeasure UnitOfMeasure { get; private set; }
    public decimal SalePrice { get; private set; }
    public decimal EstimatedCost { get; private set; }
    public bool TracksInventory { get; private set; }
    public bool AllowsFractionalQuantity { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navegación EF Core
    public Business Business { get; private set; } = null!;
    public Category? Category { get; private set; }

    private readonly List<ProductComponent> _components = new();
    public IReadOnlyCollection<ProductComponent> Components => _components.AsReadOnly();

    private Product() { }

    public static Product Create(
        Guid businessId,
        string code,
        string name,
        ProductType productType,
        UnitOfMeasure unitOfMeasure,
        decimal salePrice,
        decimal estimatedCost = 0m,
        Guid? categoryId = null,
        bool tracksInventory = true,
        bool allowsFractionalQuantity = false,
        string? description = null,
        string? barcode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (salePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(salePrice), "El precio de venta no puede ser negativo.");

        if (estimatedCost < 0)
            throw new ArgumentOutOfRangeException(nameof(estimatedCost), "El costo estimado no puede ser negativo.");

        return new Product
        {
            BusinessId = businessId,
            CategoryId = categoryId,
            Code = code.Trim().ToUpperInvariant(),
            Barcode = barcode?.Trim(),
            Name = name.Trim(),
            Description = description?.Trim(),
            ProductType = productType,
            UnitOfMeasure = unitOfMeasure,
            SalePrice = salePrice,
            EstimatedCost = estimatedCost,
            TracksInventory = tracksInventory,
            AllowsFractionalQuantity = allowsFractionalQuantity,
            IsActive = true
        };
    }

    public void Update(
        string code,
        string name,
        string? description,
        ProductType productType,
        UnitOfMeasure unitOfMeasure,
        decimal salePrice,
        decimal estimatedCost,
        Guid? categoryId,
        bool tracksInventory,
        bool allowsFractionalQuantity,
        string? barcode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (salePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(salePrice), "El precio de venta no puede ser negativo.");

        if (estimatedCost < 0)
            throw new ArgumentOutOfRangeException(nameof(estimatedCost), "El costo estimado no puede ser negativo.");

        Code = code.Trim().ToUpperInvariant();
        Barcode = barcode?.Trim();
        Name = name.Trim();
        Description = description?.Trim();
        ProductType = productType;
        UnitOfMeasure = unitOfMeasure;
        SalePrice = salePrice;
        EstimatedCost = estimatedCost;
        CategoryId = categoryId;
        TracksInventory = tracksInventory;
        AllowsFractionalQuantity = allowsFractionalQuantity;
    }

    public void Enable() => IsActive = true;
    public void Disable() => IsActive = false;
}
