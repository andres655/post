using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.Settings;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Application.Features.Settings.UpdateBusinessSettings;

public sealed class UpdateBusinessSettingsHandler(
    IAppDbContext db,
    UpdateBusinessSettingsValidator validator)
{
    public async Task<Result<BusinessSettingsDto>> HandleAsync(
        UpdateBusinessSettingsCommand command,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var error = validation.Errors[0];
            return Result.Failure<BusinessSettingsDto>(
                Error.Validation(error.PropertyName, error.ErrorMessage));
        }

        var business = await db.Businesses
            .FirstOrDefaultAsync(item => item.Id == command.BusinessId, ct);
        if (business is null)
            return Result.Failure<BusinessSettingsDto>(Error.NotFound("Business", command.BusinessId));

        var branch = await db.Branches
            .FirstOrDefaultAsync(item => item.Id == command.BranchId && item.BusinessId == command.BusinessId, ct);
        if (branch is null)
            return Result.Failure<BusinessSettingsDto>(Error.NotFound("Branch", command.BranchId));

        var settings = await db.BusinessSettings
            .FirstOrDefaultAsync(item => item.BusinessId == command.BusinessId, ct);
        if (settings is null)
        {
            settings = BusinessSettings.CreateDefault(command.BusinessId);
            db.BusinessSettings.Add(settings);
        }

        business.Update(
            command.BusinessName.Trim(),
            Clean(command.TaxId),
            Clean(command.BusinessPhone),
            Clean(command.BusinessAddress),
            command.CurrencyCode.Trim());

        branch.Update(
            command.BranchName.Trim(),
            Clean(command.BranchAddress),
            Clean(command.BranchPhone));

        settings.Update(
            command.UsesInventory,
            command.UsesProduction,
            command.UsesKitchen,
            command.UsesDelivery,
            command.UsesCustomers,
            command.UsesTaxes,
            command.AllowsCredit,
            command.AllowsNegativeInventory,
            command.CurrencySymbol.Trim(),
            Math.Round(command.DefaultTaxRate, 2, MidpointRounding.AwayFromZero),
            Clean(command.ReceiptLogoPath),
            Clean(command.ReceiptHeader),
            Clean(command.TicketFooter));

        await db.SaveChangesAsync(ct);

        return Result.Success(new BusinessSettingsDto(
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
            settings.TicketFooter));
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
