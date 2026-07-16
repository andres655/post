using SmallBusinessPOS.Domain.Common;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>Método de pago disponible para un negocio.</summary>
public class PaymentMethod : AuditableEntity
{
    public Guid BusinessId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public PaymentMethodType Type { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navegación
    public Business Business { get; private set; } = null!;

    private PaymentMethod() { }

    public static PaymentMethod Create(
        Guid businessId,
        string code,
        string name,
        PaymentMethodType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new PaymentMethod
        {
            BusinessId = businessId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Type = type,
            IsActive = true
        };
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
