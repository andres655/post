using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Infrastructure.Services;

public sealed class QuestPdfReceiptService(IAppDbContext db) : IReceiptService
{
    public async Task<byte[]> GenerateSaleReceiptAsync(Guid saleId, CancellationToken cancellationToken)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var sale = await db.Sales
            .Include(s => s.Branch)
            .Include(s => s.Details)
            .Include(s => s.Payments)
                .ThenInclude(p => p.PaymentMethod)
            .FirstOrDefaultAsync(s => s.Id == saleId, cancellationToken);

        if (sale is null)
            throw new InvalidOperationException($"Venta {saleId} no encontrada.");

        var business = await db.Businesses
            .FirstOrDefaultAsync(b => b.Id == sale.BusinessId, cancellationToken)
            ?? throw new InvalidOperationException("Negocio no encontrado.");
        var settings = await db.BusinessSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.BusinessId == sale.BusinessId, cancellationToken);
        var currencySymbol = string.IsNullOrWhiteSpace(settings?.CurrencySymbol) ? "RD$" : settings.CurrencySymbol;
        var logoPath = ResolveLogoPath(settings?.ReceiptLogoPath);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(220, 1000);
                page.Margin(12);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Content().Column(col =>
                {
                    col.Spacing(4);

                    if (logoPath is not null)
                        col.Item().AlignCenter().Width(72).Image(logoPath).FitWidth();

                    col.Item().AlignCenter().Text(business.Name).SemiBold().FontSize(12);
                    if (!string.IsNullOrWhiteSpace(business.TaxId))
                        col.Item().AlignCenter().Text($"RNC: {business.TaxId}");
                    if (!string.IsNullOrWhiteSpace(settings?.ReceiptHeader))
                        col.Item().AlignCenter().Text(settings.ReceiptHeader);
                    if (!string.IsNullOrWhiteSpace(sale.Branch.Address))
                        col.Item().AlignCenter().Text(sale.Branch.Address);

                    var receiptPhone = string.IsNullOrWhiteSpace(sale.Branch.Phone) ? business.Phone : sale.Branch.Phone;
                    if (!string.IsNullOrWhiteSpace(receiptPhone))
                        col.Item().AlignCenter().Text($"Tel: {receiptPhone}");

                    col.Item().LineHorizontal(1);

                    col.Item().Text($"Venta: {sale.ReceiptNumber}");
                    col.Item().Text($"Fecha: {sale.SoldAtUtc:yyyy-MM-dd HH:mm} UTC");
                    col.Item().Text($"Estado: {sale.Status}");
                    if (!string.IsNullOrWhiteSpace(sale.CreatedBy))
                        col.Item().Text($"Cajero: {sale.CreatedBy}");

                    col.Item().LineHorizontal(1);

                    foreach (var line in sale.Details)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(5).Text(line.ProductName);
                            row.RelativeItem(2).AlignRight().Text(line.Quantity.ToString("N2"));
                            row.RelativeItem(3).AlignRight().Text(FormatMoney(line.LineTotal, currencySymbol));
                        });
                    }

                    col.Item().LineHorizontal(1);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Subtotal");
                        row.ConstantItem(80).AlignRight().Text(FormatMoney(sale.SubTotal, currencySymbol));
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Descuento");
                        row.ConstantItem(80).AlignRight().Text(FormatMoney(sale.Discount, currencySymbol));
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("ITBIS");
                        row.ConstantItem(80).AlignRight().Text(FormatMoney(sale.Tax, currencySymbol));
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("TOTAL").SemiBold();
                        row.ConstantItem(80).AlignRight().Text(FormatMoney(sale.Total, currencySymbol)).SemiBold();
                    });

                    col.Item().LineHorizontal(1);
                    col.Item().Text("Pagos").SemiBold();

                    var paidTotal = 0m;
                    foreach (var payment in sale.Payments)
                    {
                        paidTotal += payment.TenderedAmount;
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text(payment.PaymentMethod.Name);
                            row.ConstantItem(80).AlignRight().Text(FormatMoney(payment.TenderedAmount, currencySymbol));
                        });
                    }

                    var change = sale.Payments
                        .Where(p => p.PaymentMethod.Type == PaymentMethodType.Cash)
                        .Sum(p => Math.Max(0m, p.TenderedAmount - p.Amount));

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Recibido");
                        row.ConstantItem(80).AlignRight().Text(FormatMoney(paidTotal, currencySymbol));
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Cambio");
                        row.ConstantItem(80).AlignRight().Text(FormatMoney(change, currencySymbol));
                    });

                    col.Item().PaddingTop(8).AlignCenter().Text(
                        string.IsNullOrWhiteSpace(settings?.TicketFooter)
                            ? "Gracias por su compra."
                            : settings.TicketFooter);
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string FormatMoney(decimal value, string currencySymbol) => $"{currencySymbol} {value:N2}";

    private static string? ResolveLogoPath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            return null;

        var path = configuredPath.Trim();
        if (File.Exists(path))
            return path;

        var relativePath = path.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        var contentRootPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
        if (File.Exists(contentRootPath))
            return contentRootPath;

        var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);
        return File.Exists(wwwrootPath) ? wwwrootPath : null;
    }
}
