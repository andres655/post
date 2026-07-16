namespace SmallBusinessPOS.Domain.Enums;

/// <summary>Tipos de movimiento de inventario. Toda modificación de existencias debe registrarse.</summary>
public enum MovementType
{
    /// <summary>Compra o recepción de mercancía.</summary>
    Purchase = 1,

    /// <summary>Salida por venta.</summary>
    Sale = 2,

    /// <summary>Reingreso por anulación de venta.</summary>
    SaleCancellation = 3,

    /// <summary>Ajuste manual positivo (conteo, corrección).</summary>
    AdjustmentIncrease = 4,

    /// <summary>Ajuste manual negativo (conteo, corrección).</summary>
    AdjustmentDecrease = 5,

    /// <summary>Merma, producto dañado o vencido.</summary>
    Waste = 6,

    /// <summary>Devolución de cliente o proveedor.</summary>
    Return = 7,

    /// <summary>Consumo de insumos para producción.</summary>
    ProductionInput = 8,

    /// <summary>Producción confirmada — incrementa existencias del producto terminado.</summary>
    ProductionOutput = 9,

    /// <summary>Consumo interno (personal, degustaciones).</summary>
    InternalUse = 10,

    /// <summary>Stock inicial al configurar el sistema.</summary>
    InitialStock = 11
}
