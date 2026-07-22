using SmallBusinessPOS.Domain.Common;

namespace SmallBusinessPOS.Domain.Entities;

public class Customer : AuditableEntity, ISynchronizableEntity
{
    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? DocumentNumber { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Business Business { get; private set; } = null!;

    private Customer() { }

    public static Customer Create(
        Guid businessId,
        string name,
        string? documentNumber = null,
        string? phone = null,
        string? email = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Customer
        {
            BusinessId = businessId,
            Name = name.Trim(),
            DocumentNumber = Clean(documentNumber),
            Phone = Clean(phone),
            Email = Clean(email),
            IsActive = true
        };
    }

    public void Update(string name, string? documentNumber, string? phone, string? email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        DocumentNumber = Clean(documentNumber);
        Phone = Clean(phone);
        Email = Clean(email);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
