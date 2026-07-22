using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.Settings;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Application.Features.Settings.GetBusinessSettings;

public sealed class GetBusinessSettingsHandler(IAppDbContext db)
{
    public async Task<Result<BusinessSettingsDto>> HandleAsync(
        GetBusinessSettingsQuery query,
        CancellationToken ct = default)
    {
        var business = await db.Businesses
            .FirstOrDefaultAsync(item => item.Id == query.BusinessId, ct);
        if (business is null)
            return Result.Failure<BusinessSettingsDto>(Error.NotFound("Business", query.BusinessId));

        var branch = await db.Branches
            .FirstOrDefaultAsync(item => item.Id == query.BranchId && item.BusinessId == query.BusinessId, ct);
        if (branch is null)
            return Result.Failure<BusinessSettingsDto>(Error.NotFound("Branch", query.BranchId));

        var settings = await db.BusinessSettings
            .FirstOrDefaultAsync(item => item.BusinessId == query.BusinessId, ct);

        if (settings is null)
        {
            settings = BusinessSettings.CreateDefault(query.BusinessId);
            db.BusinessSettings.Add(settings);
            await db.SaveChangesAsync(ct);
        }

        return Result.Success(ToDto(settings, business, branch));
    }

    private static BusinessSettingsDto ToDto(
        BusinessSettings settings,
        Business business,
        Branch branch) =>
        new(
            business.Id,
            branch.Id,
            business.Name,
            business.TaxId,
            business.Phone,
            business.Address,
            business.Currency,
            branch.Name,
            branch.Phone,
            branch.Address,
            settings.UsesInventory,
            settings.UsesProduction,
            settings.UsesKitchen,
            settings.UsesDelivery,
            settings.UsesCustomers,
            settings.UsesTaxes,
            settings.AllowsCredit,
            settings.AllowsNegativeInventory,
            settings.CurrencySymbol,
            settings.DefaultTaxRate,
            settings.ReceiptLogoPath,
            settings.ReceiptHeader,
            settings.TicketFooter);
}
