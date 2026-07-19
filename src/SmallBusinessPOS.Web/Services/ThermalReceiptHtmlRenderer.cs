using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Web.Services;

public static class ThermalReceiptHtmlRenderer
{
    public static async Task<string> RenderSaleAsync(
        IAppDbContext db,
        Guid saleId,
        int? widthMm,
        CancellationToken cancellationToken)
    {
        var sale = await db.Sales
            .Include(s => s.Branch)
            .Include(s => s.Details)
            .Include(s => s.Payments)
                .ThenInclude(p => p.PaymentMethod)
            .FirstOrDefaultAsync(s => s.Id == saleId, cancellationToken);

        if (sale is null)
            throw new InvalidOperationException($"Venta {saleId} no encontrada.");

        var business = await db.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == sale.BusinessId, cancellationToken)
            ?? throw new InvalidOperationException("Negocio no encontrado.");

        var settings = await db.BusinessSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.BusinessId == sale.BusinessId, cancellationToken);

        var paperWidthMm = widthMm is 58 ? 58 : 80;
        var ticketPadding = paperWidthMm == 58 ? "2.5mm" : "4mm";
        var printPadding = paperWidthMm == 58 ? "2mm" : "3mm";
        var currencySymbol = string.IsNullOrWhiteSpace(settings?.CurrencySymbol) ? "RD$" : settings.CurrencySymbol;
        var receiptPhone = string.IsNullOrWhiteSpace(sale.Branch.Phone) ? business.Phone : sale.Branch.Phone;
        var paidTotal = sale.Payments.Sum(p => p.TenderedAmount);
        var change = sale.Payments
            .Where(p => p.PaymentMethod.Type == PaymentMethodType.Cash)
            .Sum(p => Math.Max(0m, p.TenderedAmount - p.Amount));

        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"es\">");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"utf-8\" />");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        html.AppendLine($"<title>Ticket {Html(sale.ReceiptNumber)}</title>");
        html.AppendLine("<style>");
        html.AppendLine(":root { color-scheme: light; }");
        html.AppendLine("* { box-sizing: border-box; }");
        html.AppendLine("body { margin: 0; background: #f3f4f6; color: #111; font-family: Consolas, \"Courier New\", monospace; font-size: 12px; }");
        html.AppendLine(".toolbar { position: sticky; top: 0; display: flex; gap: 8px; justify-content: center; padding: 10px; background: #fff; border-bottom: 1px solid #ddd; }");
        html.AppendLine(".toolbar button { border: 1px solid #1f2937; border-radius: 6px; background: #111827; color: #fff; padding: 8px 12px; font: 600 13px system-ui, sans-serif; cursor: pointer; }");
        html.AppendLine($".ticket {{ width: {paperWidthMm}mm; min-height: 100vh; margin: 14px auto; padding: {ticketPadding}; background: #fff; }}");
        html.AppendLine(".center { text-align: center; }");
        html.AppendLine(".right { text-align: right; }");
        html.AppendLine(".muted { color: #333; }");
        html.AppendLine(".logo { max-width: 34mm; max-height: 18mm; object-fit: contain; margin: 0 auto 6px; display: block; }");
        html.AppendLine(".brand { font-size: 16px; font-weight: 700; line-height: 1.1; margin-bottom: 3px; }");
        html.AppendLine(".line { border-top: 1px dashed #111; margin: 7px 0; }");
        html.AppendLine(".row { display: grid; grid-template-columns: 1fr auto; gap: 8px; align-items: start; }");
        html.AppendLine(".item { display: grid; grid-template-columns: 1fr 14mm 20mm; gap: 4px; margin: 3px 0; }");
        html.AppendLine(".total { font-size: 15px; font-weight: 700; }");
        html.AppendLine(".footer { white-space: pre-wrap; margin-top: 10px; }");
        html.AppendLine($"@page {{ size: {paperWidthMm}mm auto; margin: 0; }}");
        html.AppendLine($"@media print {{ html, body {{ width: {paperWidthMm}mm; margin: 0; background: #fff; }} .toolbar {{ display: none; }} .ticket {{ width: {paperWidthMm}mm; min-height: 0; margin: 0; padding: {printPadding}; box-shadow: none; }} }}");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<div class=\"toolbar\"><button type=\"button\" onclick=\"window.print()\">Imprimir</button><button type=\"button\" onclick=\"window.close()\">Cerrar</button></div>");
        html.AppendLine("<main class=\"ticket\">");
        html.AppendLine(Logo(settings?.ReceiptLogoPath));
        html.AppendLine("<div class=\"center\">");
        html.AppendLine($"<div class=\"brand\">{Html(business.Name)}</div>");
        html.AppendLine(LineIf("RNC: ", business.TaxId));
        html.AppendLine(Block(settings?.ReceiptHeader));
        html.AppendLine(LineIf(string.Empty, sale.Branch.Address));
        html.AppendLine(LineIf("Tel: ", receiptPhone));
        html.AppendLine("</div>");
        html.AppendLine("<div class=\"line\"></div>");
        html.AppendLine($"<div>Venta: {Html(sale.ReceiptNumber)}</div>");
        html.AppendLine($"<div>Fecha: {Html(sale.SoldAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"))}</div>");
        html.AppendLine($"<div>Estado: {Html(sale.Status.ToString())}</div>");
        html.AppendLine(LineIf("Cajero: ", sale.CreatedBy));
        html.AppendLine("<div class=\"line\"></div>");
        html.AppendLine("<div class=\"item muted\"><div>Producto</div><div class=\"right\">Cant</div><div class=\"right\">Total</div></div>");

        foreach (var line in sale.Details)
        {
            html.AppendLine("<div class=\"item\">");
            html.AppendLine($"<div>{Html(line.ProductName)}</div>");
            html.AppendLine($"<div class=\"right\">{line.Quantity:N2}</div>");
            html.AppendLine($"<div class=\"right\">{Money(line.LineTotal, currencySymbol)}</div>");
            html.AppendLine("</div>");
        }

        html.AppendLine("<div class=\"line\"></div>");
        html.AppendLine(AmountRow("Subtotal", sale.SubTotal, currencySymbol));
        html.AppendLine(AmountRow("Descuento", sale.Discount, currencySymbol));
        html.AppendLine(AmountRow("ITBIS", sale.Tax, currencySymbol));
        html.AppendLine($"<div class=\"row total\"><div>TOTAL</div><div>{Money(sale.Total, currencySymbol)}</div></div>");
        html.AppendLine("<div class=\"line\"></div>");
        html.AppendLine("<strong>Pagos</strong>");

        foreach (var payment in sale.Payments)
            html.AppendLine(AmountRow(payment.PaymentMethod.Name, payment.TenderedAmount, currencySymbol));

        html.AppendLine(AmountRow("Recibido", paidTotal, currencySymbol));
        html.AppendLine(AmountRow("Cambio", change, currencySymbol));
        html.AppendLine("<div class=\"line\"></div>");
        html.AppendLine($"<div class=\"center footer\">{Html(string.IsNullOrWhiteSpace(settings?.TicketFooter) ? "Gracias por su compra." : settings.TicketFooter)}</div>");
        html.AppendLine("</main>");
        html.AppendLine("<script>window.addEventListener('load', function () { setTimeout(function () { window.print(); }, 350); });</script>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string Money(decimal value, string currencySymbol) => $"{Html(currencySymbol)} {value:N2}";

    private static string AmountRow(string label, decimal value, string currencySymbol) =>
        $"<div class=\"row\"><div>{Html(label)}</div><div>{Money(value, currencySymbol)}</div></div>";

    private static string LineIf(string prefix, string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : $"<div>{Html(prefix)}{Html(value)}</div>";

    private static string Block(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : $"<div>{Html(value)}</div>";

    private static string Logo(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var src = path.Trim();
        if (!src.StartsWith('/'))
            src = "/" + src.TrimStart('~', '/');

        return $"<img class=\"logo\" src=\"{Html(src)}\" alt=\"Logo\" />";
    }
}
