namespace SmallBusinessPOS.Domain.Enums;

/// <summary>Estado de una sesión de caja.</summary>
public enum CashSessionStatus
{
    Open = 1,
    Closed = 2,
    Pending = 3
}

/// <summary>Tipo de venta según punto de venta o entrega.</summary>
public enum SaleType
{
    Counter = 1,
    TakeAway = 2,
    Pickup = 3,
    Delivery = 4
}

/// <summary>Estado de una venta.</summary>
public enum SaleStatus
{
    Draft = 1,
    Confirmed = 2,
    Cancelled = 3,
    PartiallyRefunded = 4,
    Refunded = 5
}

/// <summary>Tipo de movimiento de caja (ingresos, egresos, etc).</summary>
public enum CashMovementType
{
    Opening = 1,
    SaleIncome = 2,
    OtherIncome = 3,
    Expense = 4,
    Withdrawal = 5,
    Refund = 6,
    ClosingAdjustment = 7
}
