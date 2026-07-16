using Microsoft.AspNetCore.Identity;

namespace SmallBusinessPOS.Infrastructure.Data.Identity;

/// <summary>
/// Usuario del sistema extendido con campos del negocio.
/// Pertenece al mismo contexto que las entidades de negocio.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    /// <summary>Negocio al que pertenece el usuario. Null = superadministrador.</summary>
    public Guid? BusinessId { get; set; }

    /// <summary>Sucursal asignada. Null = acceso a todas las sucursales del negocio.</summary>
    public Guid? BranchId { get; set; }

    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}".Trim();
}
