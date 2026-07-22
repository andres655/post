namespace SmallBusinessPOS.Domain.Enums;

/// <summary>Tipo de método de pago disponible.</summary>
public enum PaymentMethodType
{
    Cash = 1,
    CreditCard = 2,
    DebitCard = 3,
    BankTransfer = 4,
    Check = 5,
    Credit = 6,
    Other = 99
}
