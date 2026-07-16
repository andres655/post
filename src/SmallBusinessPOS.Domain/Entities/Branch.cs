using SmallBusinessPOS.Domain.Common;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>Sucursal de un negocio.</summary>
public class Branch : AuditableEntity
{
    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public bool IsMain { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navegación EF Core
    public Business Business { get; private set; } = null!;

    private Branch() { }

    public static Branch Create(Guid businessId, string name, bool isMain = false, string? address = null, string? phone = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Branch
        {
            BusinessId = businessId,
            Name = name.Trim(),
            IsMain = isMain,
            Address = address?.Trim(),
            Phone = phone?.Trim(),
            IsActive = true
        };
    }

    public void Update(string name, string? address, string? phone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Address = address?.Trim();
        Phone = phone?.Trim();
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
