using System.Globalization;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SmallBusinessPOS.Application.Features.Reports.GetProfitabilityReport;
using SmallBusinessPOS.Application.Features.Sales.GetDailyReport;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Infrastructure.Services;

public sealed class ReportExportService : IReportExportService
{
    public byte[] ExportDailyReportCsv(DailyReportDto report)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Seccion,Codigo,Nombre,Cantidad,Ventas,Costo,Margen,Porcentaje");
        foreach (var item in report.TopProducts)
        {
            csv.AppendLine(string.Join(',',
                Csv("Producto"),
                Csv(item.ProductCode),
                Csv(item.ProductName),
                Money(item.Quantity),
                Money(item.SalesAmount),
                Money(item.EstimatedCost),
                Money(item.GrossMargin),
                Money(item.GrossMarginPercent)));
        }

        csv.AppendLine();
        csv.AppendLine("Metrica,Valor");
        csv.AppendLine($"Ventas brutas,{Money(report.GrossSales)}");
        csv.AppendLine($"Ventas netas,{Money(report.NetSales)}");
        csv.AppendLine($"Gastos,{Money(report.Expenses)}");
        csv.AppendLine($"Efectivo esperado,{Money(report.ExpectedCash)}");

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    public byte[] ExportDailyReportPdf(DailyReportDto report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9));
                page.Header().Text($"Reporte diario - {report.Date:yyyy-MM-dd}").SemiBold().FontSize(16);
                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    AddSummary(col, [
                        ("Ventas netas", report.NetSales),
                        ("Gastos", report.Expenses),
                        ("Efectivo esperado", report.ExpectedCash),
                        ("Mermas", report.Waste)
                    ]);

                    col.Item().Text("Productos mas vendidos").SemiBold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });
                        AddHeader(table, ["Codigo", "Producto", "Cant.", "Ventas", "Costo", "Margen"]);
                        foreach (var item in report.TopProducts)
                            AddRow(table, [item.ProductCode, item.ProductName, Money(item.Quantity), Money(item.SalesAmount), Money(item.EstimatedCost), Money(item.GrossMargin)]);
                    });
                });
            });
        }).GeneratePdf();
    }

    public byte[] ExportProfitabilityReportCsv(ProfitabilityReportDto report)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Codigo,Producto,Cantidad,Ventas,Costo,Margen,Porcentaje");
        foreach (var item in report.Products)
        {
            csv.AppendLine(string.Join(',',
                Csv(item.ProductCode),
                Csv(item.ProductName),
                Money(item.Quantity),
                Money(item.SalesAmount),
                Money(item.EstimatedCost),
                Money(item.GrossMargin),
                Money(item.GrossMarginPercent)));
        }

        csv.AppendLine();
        csv.AppendLine("Fecha,Ventas,Costo,Margen,Gastos,Utilidad");
        foreach (var day in report.Daily)
        {
            csv.AppendLine(string.Join(',',
                day.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Money(day.SalesAmount),
                Money(day.EstimatedCost),
                Money(day.GrossMargin),
                Money(day.Expenses),
                Money(day.NetProfit)));
        }

        csv.AppendLine();
        csv.AppendLine("Metrica,Valor");
        csv.AppendLine($"Ventas netas,{Money(report.NetSales)}");
        csv.AppendLine($"Costo estimado,{Money(report.EstimatedCost)}");
        csv.AppendLine($"Margen bruto,{Money(report.GrossMargin)}");
        csv.AppendLine($"Gastos,{Money(report.Expenses)}");
        csv.AppendLine($"Utilidad estimada,{Money(report.NetProfit)}");

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    public byte[] ExportProfitabilityReportPdf(ProfitabilityReportDto report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9));
                page.Header().Text($"Rentabilidad - {report.From:yyyy-MM-dd} a {report.To:yyyy-MM-dd}").SemiBold().FontSize(16);
                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    AddSummary(col, [
                        ("Ventas netas", report.NetSales),
                        ("Costo estimado", report.EstimatedCost),
                        ("Margen bruto", report.GrossMargin),
                        ("Gastos", report.Expenses),
                        ("Utilidad", report.NetProfit)
                    ]);

                    col.Item().Text("Rentabilidad por producto").SemiBold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });
                        AddHeader(table, ["Codigo", "Producto", "Cant.", "Ventas", "Costo", "Margen"]);
                        foreach (var item in report.Products)
                            AddRow(table, [item.ProductCode, item.ProductName, Money(item.Quantity), Money(item.SalesAmount), Money(item.EstimatedCost), Money(item.GrossMargin)]);
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void AddSummary(ColumnDescriptor col, IReadOnlyList<(string Label, decimal Value)> values)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var _ in values)
                    columns.RelativeColumn();
            });

            foreach (var item in values)
            {
                table.Cell().Border(1).Padding(6).Column(cell =>
                {
                    cell.Item().Text(item.Label);
                    cell.Item().Text($"RD$ {Money(item.Value)}").SemiBold();
                });
            }
        });
    }

    private static void AddHeader(TableDescriptor table, IReadOnlyList<string> headers)
    {
        foreach (var header in headers)
            table.Cell().Background("#f1f3f5").Padding(4).Text(header).SemiBold();
    }

    private static void AddRow(TableDescriptor table, IReadOnlyList<string> values)
    {
        foreach (var value in values)
            table.Cell().BorderBottom(0.5f).BorderColor("#dee2e6").Padding(4).Text(value);
    }

    private static string Csv(string value)
        => "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private static string Money(decimal value)
        => value.ToString("0.00", CultureInfo.InvariantCulture);
}
