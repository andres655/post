using Microsoft.AspNetCore.Components;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.Reports.GetManagementDashboard;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Web.Components.Pages.Dashboard;

public partial class Home
{
    [Inject] private GetPosContextHandler PosContextHandler { get; set; } = null!;
    [Inject] private GetManagementDashboardHandler DashboardHandler { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IClock Clock { get; set; } = null!;

    private bool _loading = true;
    private string? _error;
    private ManagementDashboardDto? _dashboard;

    private string TodayLabel => Clock.UtcNow.ToLocalTime().ToString("dd 'de' MMMM");
    private int CurrentHour => Math.Max(1, Clock.UtcNow.ToLocalTime().Hour);

    private decimal AverageTicket => _dashboard is null || _dashboard.Kpis.TodaySalesCount == 0
        ? 0
        : _dashboard.Kpis.TodaySales / _dashboard.Kpis.TodaySalesCount;

    private decimal TransactionsPerHour => _dashboard is null
        ? 0
        : _dashboard.Kpis.TodaySalesCount / Math.Max(1m, CurrentHour);

    private string WeeklyShareLabel => _dashboard is null || _dashboard.Kpis.WeekSales <= 0
        ? "0.0%"
        : $"{Math.Min(99, (_dashboard.Kpis.TodaySales / _dashboard.Kpis.WeekSales) * 100):N1}%";

    private IEnumerable<DashboardActivityDto> RecentRows => _dashboard?.RecentActivity.Take(6) ?? [];

    // Temporary visual projection until Application exposes daily sales buckets.
    private IReadOnlyList<(string Label, decimal Height)> WeekBars
    {
        get
        {
            var today = _dashboard?.Kpis.TodaySales ?? 0;
            var week = _dashboard?.Kpis.WeekSales ?? 0;
            var average = week <= 0 ? today : week / 7m;
            var values = new[] { average * .62m, average * .78m, average * .7m, average * .86m, today, average * .66m, average * .5m };
            var max = Math.Max(1m, values.Max());
            var labels = new[] { "Lun", "Mar", "Mie", "Jue", "Vie", "Sab", "Dom" };
            return values.Select((value, index) => (labels[index], Math.Clamp((value / max) * 100, 8, 100))).ToList();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardAsync();
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
            _error = "No se pudo cargar el panel de control.";
        }
        finally
        {
            _loading = false;
        }
    }

    private string Money(decimal value) => $"RD$ {value:N2}";

    private void GoToPos() => Navigation.NavigateTo("/pos");
    private void GoToCash() => Navigation.NavigateTo("/cash/current");
    private void GoToProducts() => Navigation.NavigateTo("/products");
    private void GoToDailyReport() => Navigation.NavigateTo("/reports/daily");
}
