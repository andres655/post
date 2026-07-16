using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmallBusinessPOS.Application.Interfaces;

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

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(220, 1000); // Aproximado 80mm roll
                page.Margin(12);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Content().Column(col =>
                {
                    col.Spacing(4);

                    col.Item().AlignCenter().Text(business.Name).SemiBold().FontSize(12);
                    if (!string.IsNullOrWhiteSpace(sale.Branch.Address))
                        col.Item().AlignCenter().Text(sale.Branch.Address);
                    if (!string.IsNullOrWhiteSpace(sale.Branch.Phone))
                        col.Item().AlignCenter().Text($"Tel: {sale.Branch.Phone}");

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
                            row.RelativeItem(3).AlignRight().Text(line.LineTotal.ToString("N2"));
                        });
                    }

                    col.Item().LineHorizontal(1);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Subtotal");
                        row.ConstantItem(80).AlignRight().Text(sale.SubTotal.ToString("N2"));
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Descuento");
                        row.ConstantItem(80).AlignRight().Text(sale.Discount.ToString("N2"));
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("ITBIS");
                        row.ConstantItem(80).AlignRight().Text(sale.Tax.ToString("N2"));
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("TOTAL").SemiBold();
                        row.ConstantItem(80).AlignRight().Text(sale.Total.ToString("N2")).SemiBold();
                    });

                    col.Item().LineHorizontal(1);
                    col.Item().Text("Pagos").SemiBold();

                    var paidTotal = 0m;
                    foreach (var payment in sale.Payments)
                    {
                        paidTotal += payment.Amount;
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text(payment.PaymentMethod.Name);
                            row.ConstantItem(80).AlignRight().Text(payment.Amount.ToString("N2"));
                        });
                    }

                    var cashPaid = sale.Payments
                        .Where(p => p.PaymentMethod.Type == Domain.Enums.PaymentMethodType.Cash)
                        .Sum(p => p.Amount);
                    var change = cashPaid > sale.Total ? cashPaid - sale.Total : 0m;

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Recibido");
                        row.ConstantItem(80).AlignRight().Text(paidTotal.ToString("N2"));
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Cambio");
                        row.ConstantItem(80).AlignRight().Text(change.ToString("N2"));
                    });

                    col.Item().PaddingTop(8).AlignCenter().Text("¡Gracias por su compra!");
                });
            });
        });

        return document.GeneratePdf();
    }
}
