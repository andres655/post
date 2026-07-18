using SmallBusinessPOS.Domain.Common;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>
/// Representa un negocio (empresa) en el sistema multiempresa.
/// Raíz de agregado.
/// </summary>
public class Business : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? TaxId { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public string Currency { get; private set; } = "DOP";
    public string TimeZone { get; private set; } = "America/Santo_Domingo";
    public BusinessType BusinessType { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navegación EF Core
    private readonly List<Branch> _branches = new();
    public IReadOnlyCollection<Branch> Branches => _branches.AsReadOnly();

    private Business() { }

    public static Business Create(
        string name,
        string currency,
        string timeZone,
        BusinessType businessType,
        string? taxId = null,
        string? phone = null,
        string? address = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        return new Business
        {
            Name = name.Trim(),
            Currency = currency.Trim().ToUpperInvariant(),
            TimeZone = timeZone,
            BusinessType = businessType,
            TaxId = taxId?.Trim(),
            Phone = phone?.Trim(),
            Address = address?.Trim(),
            IsActive = true
        };
    }

    public void Update(string name, string? taxId, string? phone, string? address, string? currency = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        TaxId = taxId?.Trim();
        Phone = phone?.Trim();
        Address = address?.Trim();
        if (!string.IsNullOrWhiteSpace(currency))
            Currency = currency.Trim().ToUpperInvariant();
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
