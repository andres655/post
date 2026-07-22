using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Receipts.GetSaleReceipt;

public sealed class GetSaleReceiptHandler(IAppDbContext db)
{
    public async Task<Result<SaleReceiptDto>> HandleAsync(
        GetSaleReceiptQuery query,
        CancellationToken ct = default)
    {
        var sale = await db.Sales
            .AsNoTracking()
            .Include(s => s.Branch)
            .Include(s => s.Details)
            .Include(s => s.Payments)
                .ThenInclude(p => p.PaymentMethod)
            .FirstOrDefaultAsync(s => s.Id == query.SaleId, ct);

        if (sale is null)
            return Result.Failure<SaleReceiptDto>(Error.NotFound("Sale", query.SaleId));

        var business = await db.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == sale.BusinessId, ct);

        if (business is null)
            return Result.Failure<SaleReceiptDto>(Error.NotFound("Business", sale.BusinessId));

        var settings = await db.BusinessSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.BusinessId == sale.BusinessId, ct);

        var currencySymbol = string.IsNullOrWhiteSpace(settings?.CurrencySymbol)
            ? "RD$"
            : settings.CurrencySymbol;
        var receiptPhone = string.IsNullOrWhiteSpace(sale.Branch.Phone)
            ? business.Phone
            : sale.Branch.Phone;
        var paidTotal = sale.Payments.Sum(payment => payment.TenderedAmount);
        var change = sale.Payments
            .Where(payment => payment.PaymentMethod.Type == PaymentMethodType.Cash)
            .Sum(payment => Math.Max(0m, payment.TenderedAmount - payment.Amount));

        var dto = new SaleReceiptDto(
            sale.Id,
            sale.BusinessId,
            sale.BranchId,
            sale.ReceiptNumber,
            sale.SoldAtUtc,
            sale.Status.ToString(),
            sale.CreatedBy,
            sale.SubTotal,
            sale.Discount,
            sale.Tax,
            sale.Total,
            business.Name,
            business.TaxId,
            sale.Branch.Address,
            receiptPhone,
            settings?.ReceiptLogoPath,
            settings?.ReceiptHeader,
            settings?.TicketFooter,
            currencySymbol,
            paidTotal,
            change,
            sale.Details
                .Select(line => new SaleReceiptLineDto(
                    line.ProductName,
                    line.Quantity,
                    line.LineTotal))
                .ToList(),
            sale.Payments
                .Select(payment => new SaleReceiptPaymentDto(
                    payment.PaymentMethod.Name,
                    payment.Amount,
                    payment.TenderedAmount))
                .ToList());

        return Result.Success(dto);
    }
}
