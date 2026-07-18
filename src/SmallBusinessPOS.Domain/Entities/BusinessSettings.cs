using SmallBusinessPOS.Domain.Common;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>
/// Configuración de módulos y comportamiento por negocio.
/// Permite activar/desactivar funcionalidades sin código adicional.
/// </summary>
public class BusinessSettings : Entity
{
    public Guid BusinessId { get; private set; }
    public bool UsesInventory { get; private set; } = true;
    public bool UsesProduction { get; private set; }
    public bool UsesKitchen { get; private set; }
    public bool UsesDelivery { get; private set; }
    public bool UsesCustomers { get; private set; }
    public bool UsesTaxes { get; private set; }
    public bool AllowsCredit { get; private set; }
    public bool AllowsNegativeInventory { get; private set; }
    public string CurrencySymbol { get; private set; } = "RD$";
    public decimal DefaultTaxRate { get; private set; }
    public string? ReceiptLogoPath { get; private set; }
    public string? ReceiptHeader { get; private set; }
    public string? TicketFooter { get; private set; }

    // Navegación EF Core
    public Business Business { get; private set; } = null!;

    private BusinessSettings() { }

    public static BusinessSettings CreateDefault(Guid businessId)
    {
        return new BusinessSettings
        {
            BusinessId = businessId,
            UsesInventory = true,
            UsesProduction = true,
            UsesKitchen = false,
            UsesDelivery = false,
            UsesCustomers = false,
            UsesTaxes = false,
            AllowsCredit = false,
            AllowsNegativeInventory = false,
            CurrencySymbol = "RD$",
            DefaultTaxRate = 0m,
            ReceiptLogoPath = null,
            ReceiptHeader = null,
            TicketFooter = "¡Gracias por su preferencia!"
        };
    }

    public void Update(
        bool usesInventory,
        bool usesProduction,
        bool usesKitchen,
        bool usesDelivery,
        bool usesCustomers,
        bool usesTaxes,
        bool allowsCredit,
        bool allowsNegativeInventory,
        string currencySymbol,
        decimal defaultTaxRate,
        string? receiptLogoPath,
        string? receiptHeader,
        string? ticketFooter)
    {
        UsesInventory = usesInventory;
        UsesProduction = usesProduction;
        UsesKitchen = usesKitchen;
        UsesDelivery = usesDelivery;
        UsesCustomers = usesCustomers;
        UsesTaxes = usesTaxes;
        AllowsCredit = allowsCredit;
        AllowsNegativeInventory = allowsNegativeInventory;
        CurrencySymbol = currencySymbol;
        DefaultTaxRate = defaultTaxRate;
        ReceiptLogoPath = string.IsNullOrWhiteSpace(receiptLogoPath) ? null : receiptLogoPath.Trim();
        ReceiptHeader = string.IsNullOrWhiteSpace(receiptHeader) ? null : receiptHeader.Trim();
        TicketFooter = ticketFooter;
    }
}
