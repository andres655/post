using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.Users.ChangeUserStatus;
using SmallBusinessPOS.Application.Features.Users.CreateUser;
using SmallBusinessPOS.Application.Features.Users.DTOs;
using SmallBusinessPOS.Application.Features.Users.GetUsers;
using SmallBusinessPOS.Application.Features.Users.ResetUserPassword;
using SmallBusinessPOS.Application.Features.Users.UpdateUserRoles;
using System.Security.Claims;

namespace SmallBusinessPOS.Web.Components.Pages.Admin;

public partial class Users
{
    private PosContextDto? _context;
    private readonly List<UserAdministrationRowDto> _users = [];
    private List<string> _availableRoles = [];
    private string? _currentUserId;

    private bool _loading = true;
    private bool _saving;
    private string? _errorMessage;
    private string? _successMessage;

    private bool _mostrarFormulario;
    private UserForm _form = new();
    private string? _formError;

    private UserAdministrationRowDto? _editingUser;
    private Dictionary<string, bool> _selectedRoles = [];
    private string? _rolesError;

    private UserAdministrationRowDto? _resetUser;
    private string _newPassword = string.Empty;
    private string? _resetError;

    private UserAdministrationRowDto? _estadoUser;

    protected override async Task OnInitializedAsync()
    {
        await CargarAsync();
    }

    private async Task CargarAsync()
    {
        _loading = true;
        _errorMessage = null;

        try
        {
            var auth = await AuthState.GetAuthenticationStateAsync();
            _currentUserId = auth.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await CargarContextoAsync();
            await CargarUsuariosAsync();
        }
        catch
        {
            _errorMessage = "No se pudo cargar la gestion de usuarios.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task CargarContextoAsync()
    {
        var result = await PosContextHandler.HandleAsync(new GetPosContextQuery());
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error.Description);

        _context = result.Value;
    }

    private async Task CargarRolesAsync()
    {
        if (_context is null)
            return;

        var result = await GetUsersHandler.HandleAsync(new GetUsersQuery(_context.BusinessId, _currentUserId));
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error.Description);

        _availableRoles = result.Value.AvailableRoles;
        _users.Clear();
        _users.AddRange(result.Value.Users);
    }

    private async Task CargarUsuariosAsync()
    {
        await CargarRolesAsync();
    }

    private void NuevoUsuario()
    {
        _form = new UserForm { Role = _availableRoles.FirstOrDefault() ?? "Cashier" };
        _formError = null;
        _mostrarFormulario = true;
    }

    private void CerrarFormulario()
    {
        _mostrarFormulario = false;
        _formError = null;
        _saving = false;
    }

    private async Task CrearUsuario()
    {
        _saving = true;
        _formError = null;

        try
        {
            if (_context is null)
            {
                _formError = "No se pudo determinar el negocio activo.";
                return;
            }

            if (string.IsNullOrWhiteSpace(_form.Email) || string.IsNullOrWhiteSpace(_form.Password) || string.IsNullOrWhiteSpace(_form.Role))
            {
                _formError = "Email, clave temporal y rol son obligatorios.";
                return;
            }

            var result = await CreateUserHandler.HandleAsync(new CreateUserCommand(
                _context.BusinessId,
                _context.BranchId,
                _form.FirstName,
                _form.LastName,
                _form.Email,
                _form.Password,
                _form.Role));
            if (result.IsFailure)
            {
                _formError = result.Error.Description;
                return;
            }

            _successMessage = "Usuario creado correctamente.";
            CerrarFormulario();
            await CargarUsuariosAsync();
        }
        catch
        {
            _formError = "No se pudo crear el usuario.";
        }
        finally
        {
            _saving = false;
        }
    }

    private void AbrirRoles(UserAdministrationRowDto user)
    {
        _editingUser = user;
        _rolesError = null;
        _selectedRoles = _availableRoles.ToDictionary(role => role, role => user.Roles.Contains(role));
    }

    private void CerrarRoles()
    {
        _editingUser = null;
        _rolesError = null;
        _selectedRoles = [];
        _saving = false;
    }

    private bool RoleMarcado(string role) =>
        _selectedRoles.TryGetValue(role, out var selected) && selected;

    private void CambiarRol(string role, ChangeEventArgs args)
    {
        _selectedRoles[role] = args.Value is bool selected && selected;
    }

    private async Task GuardarRoles()
    {
        if (_editingUser is null)
            return;

        _saving = true;
        _rolesError = null;

        try
        {
            var selected = _selectedRoles
                .Where(x => x.Value)
                .Select(x => x.Key)
                .ToList();

            if (selected.Count == 0)
            {
                _rolesError = "El usuario debe tener al menos un rol.";
                return;
            }

            if (_editingUser.IsCurrentUser && !selected.Contains("Administrator"))
            {
                _rolesError = "No puedes quitarte tu propio rol de administrador.";
                return;
            }

            var result = await UpdateUserRolesHandler.HandleAsync(new UpdateUserRolesCommand(
                _editingUser.Id,
                selected,
                _editingUser.IsCurrentUser));
            if (result.IsFailure)
            {
                _rolesError = result.Error.Description;
                return;
            }

            _successMessage = "Roles actualizados.";
            CerrarRoles();
            await CargarUsuariosAsync();
        }
        catch
        {
            _rolesError = "No se pudieron actualizar los roles.";
        }
        finally
        {
            _saving = false;
        }
    }

    private void AbrirResetClave(UserAdministrationRowDto user)
    {
        _resetUser = user;
        _newPassword = string.Empty;
        _resetError = null;
    }

    private void CerrarResetClave()
    {
        _resetUser = null;
        _newPassword = string.Empty;
        _resetError = null;
        _saving = false;
    }

    private async Task ResetearClave()
    {
        if (_resetUser is null)
            return;

        _saving = true;
        _resetError = null;

        try
        {
            if (string.IsNullOrWhiteSpace(_newPassword))
            {
                _resetError = "La nueva clave es obligatoria.";
                return;
            }

            var result = await ResetPasswordHandler.HandleAsync(new ResetUserPasswordCommand(
                _resetUser.Id,
                _newPassword));
            if (result.IsFailure)
            {
                _resetError = result.Error.Description;
                return;
            }

            _successMessage = "Clave actualizada.";
            CerrarResetClave();
        }
        catch
        {
            _resetError = "No se pudo resetear la clave.";
        }
        finally
        {
            _saving = false;
        }
    }

    private void ConfirmarEstado(UserAdministrationRowDto user)
    {
        if (user.IsCurrentUser)
        {
            _errorMessage = "No puedes desactivar tu propia sesion.";
            return;
        }

        _estadoUser = user;
    }

    private async Task CambiarEstado()
    {
        if (_estadoUser is null)
            return;

        _saving = true;
        _errorMessage = null;

        try
        {
            var result = await ChangeStatusHandler.HandleAsync(new ChangeUserStatusCommand(
                _estadoUser.Id,
                _estadoUser.IsActive,
                _estadoUser.IsCurrentUser));
            if (result.IsFailure)
            {
                _errorMessage = result.Error.Description;
                return;
            }

            _successMessage = result.Value ? "Usuario activado." : "Usuario desactivado.";
            _estadoUser = null;
            await CargarUsuariosAsync();
        }
        catch
        {
            _errorMessage = "No se pudo cambiar el estado del usuario.";
        }
        finally
        {
            _saving = false;
        }
    }

    private static string RoleLabel(string role) => role switch
    {
        "Administrator" => "Administrador",
        "Supervisor" => "Supervisor",
        "Cashier" => "Cajero",
        _ => role
    };

    private static string Initials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "U";

        var parts = value
            .Split(['@', '.', ' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0]));

        return string.Concat(parts);
    }

    private sealed class UserForm
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Cashier";
    }
}
