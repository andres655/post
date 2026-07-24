using Microsoft.AspNetCore.Components;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.Reports.GetManagementDashboard;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Web.Components.Pages.Dashboard;

public partial class Analytics
{
    [Inject] private GetPosContextHandler PosContextHandler { get; set; } = null!;
    [Inject] private GetManagementDashboardHandler DashboardHandler { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ICurrentUserService CurrentUser { get; set; } = null!;
    [Inject] private IClock Clock { get; set; } = null!;

    private bool _loading = true;
    private bool _showGuidance = true;
    private string? _error;
    private ManagementDashboardDto? _dashboard;
    private string _guidanceText = "Aqui veras tus KPIs del dia y tus acciones mas importantes.";

    private decimal AverageTicket => _dashboard is null || _dashboard.Kpis.TodaySalesCount == 0
        ? 0
        : _dashboard.Kpis.TodaySales / _dashboard.Kpis.TodaySalesCount;

    private decimal CompositionTotal => _dashboard is null
        ? 0
        : Math.Abs(_dashboard.Kpis.TodaySales) + Math.Abs(_dashboard.Kpis.TodayNetProfit) + Math.Abs(_dashboard.Kpis.TodayExpenses);

    private decimal SalesTransactionRatio => _dashboard is null || _dashboard.Kpis.WeekSales <= 0
        ? 0
        : Math.Min(99, (_dashboard.Kpis.TodaySales / _dashboard.Kpis.WeekSales) * 100);

    private decimal NetProfitRatio => _dashboard is null || _dashboard.Kpis.WeekSales <= 0
        ? 0
        : (_dashboard.Kpis.WeekNetProfit / _dashboard.Kpis.WeekSales) * 100;

    private string DonutStyle
    {
        get
        {
            if (_dashboard is null || CompositionTotal <= 0)
                return "background: conic-gradient(#e5e7eb 0 100%);";

            var sales = PercentOf(_dashboard.Kpis.TodaySales, CompositionTotal);
            var profit = PercentOf(_dashboard.Kpis.TodayNetProfit, CompositionTotal);
            var salesEnd = sales;
            var profitEnd = sales + profit;
            return $"background: conic-gradient(#020617 0 {salesEnd:N2}%, #007f55 {salesEnd:N2}% {profitEnd:N2}%, #f59e0b {profitEnd:N2}% 100%);";
        }
    }

    protected override async Task OnInitializedAsync()
    {
        _loading = true;

        try
        {
            await LoadDashboardAsync();
            await ConfigureGuidanceAsync();
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task ConfigureGuidanceAsync()
    {
        if (await CurrentUser.IsInRoleAsync("Administrator"))
            _guidanceText = "Administra usuarios, catalogo y caja desde los indicadores principales.";
        else if (await CurrentUser.IsInRoleAsync("Supervisor"))
            _guidanceText = "Revisa inventario, produccion y rentabilidad para mantener control operativo.";
        else
            _guidanceText = "Abre la caja y usa el punto de venta para registrar operaciones diarias.";
    }

    private async Task LoadDashboardAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            var context = await PosContextHandler.HandleAsync(new GetPosContextQuery());
            if (context.IsFailure)
            {
                _error = context.Error.Description;
                return;
            }

            var result = await DashboardHandler.HandleAsync(new GetManagementDashboardQuery(
                context.Value.BusinessId,
                context.Value.BranchId,
                Clock.TodayUtc));

            if (result.IsFailure)
                _error = result.Error.Description;
            else
                _dashboard = result.Value;
        }
        catch
        {
            _error = "No se pudo cargar el dashboard.";
        }
        finally
        {
            _loading = false;
        }
    }

    private string Money(decimal value) => $"RD$ {value:N2}";

    private static string CompactMoney(decimal value)
    {
        var abs = Math.Abs(value);
        if (abs >= 1_000_000)
            return $"RD$ {(value / 1_000_000):N1}M";
        if (abs >= 1_000)
            return $"RD$ {(value / 1_000):N0}K";

        return $"RD$ {value:N0}";
    }

    private static decimal PercentOf(decimal value, decimal total)
    {
        if (total <= 0)
            return 0;

        return Math.Clamp((Math.Abs(value) / total) * 100, 0, 100);
    }

    private static decimal ProgressWidth(decimal value, decimal total)
    {
        if (total <= 0)
            return 0;

        return Math.Clamp((Math.Abs(value) / Math.Abs(total)) * 100, 4, 100);
    }

    private decimal ProductBarWidth(DashboardProductDto item)
    {
        if (_dashboard is null || _dashboard.TopProducts.Count == 0)
            return 0;

        var max = _dashboard.TopProducts.Max(product => product.Quantity);
        return ProgressWidth(item.Quantity, max);
    }

    private void GoToDailyReport() => Navigation.NavigateTo("/reports/daily");
    private void GoToProfitability() => Navigation.NavigateTo("/reports/profitability");
    private void HideGuidance() => _showGuidance = false;

    private void ExportProfitabilityCsv()
    {
        var to = Clock.TodayUtc;
        var from = to.AddDays(-6);
        Navigation.NavigateTo($"/api/reports/profitability/{from:yyyy-MM-dd}/{to:yyyy-MM-dd}/csv", forceLoad: true);
    }
}
