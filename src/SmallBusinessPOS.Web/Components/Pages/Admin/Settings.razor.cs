using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.Settings;
using SmallBusinessPOS.Application.Features.Settings.GetBusinessSettings;
using SmallBusinessPOS.Application.Features.Settings.UpdateBusinessSettings;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Web.Components.Pages.Admin;

public partial class Settings
{
    private static readonly HashSet<string> LogoAllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp"
    };

    private const long MaxLogoBytes = 1_000_000;

    private bool _loading = true;
    private bool _saving;
    private string? _error;
    private string? _success;
    private PosContextDto? _context;
    private BusinessSettingsDto? _settings;
    private SettingsForm _form = new();
    private string? LogoPreviewUrl => string.IsNullOrWhiteSpace(_form.ReceiptLogoPath)
        ? null
        : "/" + _form.ReceiptLogoPath.Replace("\\", "/").TrimStart('/');

    [Inject] private GetPosContextHandler PosContextHandler { get; set; } = null!;
    [Inject] private GetBusinessSettingsHandler GetSettingsHandler { get; set; } = null!;
    [Inject] private UpdateBusinessSettingsHandler UpdateSettingsHandler { get; set; } = null!;
    [Inject] private IFileStorageService FileStorage { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        _success = null;

        try
        {
            var contextResult = await PosContextHandler.HandleAsync(new GetPosContextQuery());
            if (contextResult.IsFailure)
            {
                _error = contextResult.Error.Description;
                return;
            }

            _context = contextResult.Value;
            var settingsResult = await GetSettingsHandler.HandleAsync(
                new GetBusinessSettingsQuery(_context.BusinessId, _context.BranchId));
            if (settingsResult.IsFailure)
            {
                _error = settingsResult.Error.Description;
                return;
            }

            _settings = settingsResult.Value;
            _form = SettingsForm.FromSettings(_settings);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task SaveAsync()
    {
        if (_context is null || _saving)
            return;

        _saving = true;
        _error = null;
        _success = null;

        try
        {
            var result = await UpdateSettingsHandler.HandleAsync(new UpdateBusinessSettingsCommand(
                _context.BusinessId,
                _context.BranchId,
                _form.BusinessName,
                _form.TaxId,
                _form.BusinessPhone,
                _form.BusinessAddress,
                _form.CurrencyCode,
                _form.BranchName,
                _form.BranchPhone,
                _form.BranchAddress,
                _form.UsesInventory,
                _form.UsesProduction,
                _form.UsesKitchen,
                _form.UsesDelivery,
                _form.UsesCustomers,
                _form.UsesTaxes,
                _form.AllowsCredit,
                _form.AllowsNegativeInventory,
                _form.CurrencySymbol,
                _form.DefaultTaxRate,
                _form.ReceiptLogoPath,
                _form.ReceiptHeader,
                _form.TicketFooter));

            if (result.IsFailure)
            {
                _error = result.Error.Description;
                return;
            }

            _settings = result.Value;
            _form = SettingsForm.FromSettings(_settings);
            _success = "Configuracion guardada correctamente.";
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task OnLogoSelectedAsync(InputFileChangeEventArgs e)
    {
        _error = null;
        _success = null;

        var result = await UploadLogoAsync(e.File);
        if (!result.IsSuccess)
        {
            _error = result.Error;
            return;
        }

        _form.ReceiptLogoPath = result.Path;
        _success = "Logo cargado. Guarda la configuracion para usarlo en el ticket.";
    }

    private async Task<LogoUploadResult> UploadLogoAsync(IBrowserFile file)
    {
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!LogoAllowedExtensions.Contains(extension))
        {
            return LogoUploadResult.Failure("El logo debe ser una imagen PNG, JPG o WEBP.");
        }

        if (file.Size > MaxLogoBytes)
        {
            return LogoUploadResult.Failure("El logo no puede pesar mas de 1 MB.");
        }

        try
        {
            await using var input = file.OpenReadStream(MaxLogoBytes);
            var path = await FileStorage.SaveAsync(
                input,
                "ticket-logo" + extension,
                "uploads/logos",
                LogoAllowedExtensions,
                MaxLogoBytes);

            return LogoUploadResult.Success(path);
        }
        catch (InvalidOperationException ex)
        {
            return LogoUploadResult.Failure(ex.Message);
        }
        catch (IOException)
        {
            return LogoUploadResult.Failure("No se pudo guardar el logo. Intenta nuevamente.");
        }
    }

    private void RemoveLogo()
    {
        _form.ReceiptLogoPath = null;
    }

    private sealed record LogoUploadResult(bool IsSuccess, string? Path, string? Error)
    {
        public static LogoUploadResult Success(string path) => new(true, path, null);

        public static LogoUploadResult Failure(string error) => new(false, null, error);
    }

    private sealed class SettingsForm
    {
        public string BusinessName { get; set; } = string.Empty;
        public string? TaxId { get; set; }
        public string? BusinessPhone { get; set; }
        public string? BusinessAddress { get; set; }
        public string CurrencyCode { get; set; } = "DOP";
        public string BranchName { get; set; } = string.Empty;
        public string? BranchPhone { get; set; }
        public string? BranchAddress { get; set; }
        public bool UsesInventory { get; set; }
        public bool UsesProduction { get; set; }
        public bool UsesKitchen { get; set; }
        public bool UsesDelivery { get; set; }
        public bool UsesCustomers { get; set; }
        public bool UsesTaxes { get; set; }
        public bool AllowsCredit { get; set; }
        public bool AllowsNegativeInventory { get; set; }
        public string CurrencySymbol { get; set; } = "RD$";
        public decimal DefaultTaxRate { get; set; }
        public string? ReceiptLogoPath { get; set; }
        public string? ReceiptHeader { get; set; }
        public string? TicketFooter { get; set; }

        public static SettingsForm FromSettings(BusinessSettingsDto settings) => new()
        {
            BusinessName = settings.BusinessName,
            TaxId = settings.TaxId,
            BusinessPhone = settings.BusinessPhone,
            BusinessAddress = settings.BusinessAddress,
            CurrencyCode = settings.CurrencyCode,
            BranchName = settings.BranchName,
            BranchPhone = settings.BranchPhone,
            BranchAddress = settings.BranchAddress,
            UsesInventory = settings.UsesInventory,
            UsesProduction = settings.UsesProduction,
            UsesKitchen = settings.UsesKitchen,
            UsesDelivery = settings.UsesDelivery,
            UsesCustomers = settings.UsesCustomers,
            UsesTaxes = settings.UsesTaxes,
            AllowsCredit = settings.AllowsCredit,
            AllowsNegativeInventory = settings.AllowsNegativeInventory,
            CurrencySymbol = settings.CurrencySymbol,
            DefaultTaxRate = settings.DefaultTaxRate,
            ReceiptLogoPath = settings.ReceiptLogoPath,
            ReceiptHeader = settings.ReceiptHeader,
            TicketFooter = settings.TicketFooter
        };
    }
}
