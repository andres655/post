namespace SmallBusinessPOS.Domain.Enums;

/// <summary>
/// Tipo de producto. Determina comportamiento en ventas, inventario y producción.
/// </summary>
public enum ProductType
{
    /// <summary>Producto estándar con o sin control de inventario.</summary>
    Standard = 1,

    /// <summary>Producto elaborado internamente (ej: pollo horneado).</summary>
    PreparedItem = 2,

    /// <summary>Combinación de varios productos vendidos como unidad.</summary>
    Combo = 3,

    /// <summary>Servicio no físico (ej: entrega a domicilio).</summary>
    Service = 4,

    /// <summary>Insumo o ingrediente. No se vende directamente.</summary>
    Ingredient = 5,

    /// <summary>Material de empaque (ej: cajas, bolsas).</summary>
    Packaging = 6
}
