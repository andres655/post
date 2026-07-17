using SmallBusinessPOS.Application.Features.Reports.GetProfitabilityReport;
using SmallBusinessPOS.Application.Features.Sales.GetDailyReport;

namespace SmallBusinessPOS.Application.Interfaces;

public interface IReportExportService
{
    byte[] ExportDailyReportCsv(DailyReportDto report);
    byte[] ExportDailyReportPdf(DailyReportDto report);
    byte[] ExportProfitabilityReportCsv(ProfitabilityReportDto report);
    byte[] ExportProfitabilityReportPdf(ProfitabilityReportDto report);
}
