using Microsoft.AspNetCore.Components;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.Reports.GetManagementDashboard;
using SmallBusinessPOS.Application.Features.Settings.GetBusinessSettings;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Web.Components.Pages.Dashboard;

public partial class DayStatus
{
    [Inject] private GetPosContextHandler PosContextHandler { get; set; } = null!;
    [Inject] private GetManagementDashboardHandler DashboardHandler { get; set; } = null!;
    [Inject] private GetBusinessSettingsHandler GetSettingsHandler { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IClock Clock { get; set; } = null!;

    private bool _loading = true;
    private string? _error;
    private PosContextDto? _context;
    private ManagementDashboardDto? _dashboard;
    private string _currencySymbol = "RD$";
    private const int LowStockDisplayLimit = 5;
    private IEnumerable<DashboardLowStockDto> VisibleLowStockItems =>
        _dashboard?.LowStockItems.Take(LowStockDisplayLimit) ?? [];
    private string LowStockCountText
    {
        get
        {
            var total = _dashboard?.LowStockItems.Count ?? 0;
            if (total == 0)
                return "0 visibles";

            var visible = Math.Min(total, LowStockDisplayLimit);
            return total > visible
                ? $"{visible} de {total} visibles"
                : $"{visible} visibles";
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            var contextResult = await PosContextHandler.HandleAsync(new GetPosContextQuery());
            if (contextResult.IsFailure)
            {
                _error = contextResult.Error.Description;
                return;
            }

            _context = contextResult.Value;
            var settingsResult = await GetSettingsHandler.HandleAsync(
                new GetBusinessSettingsQuery(_context.BusinessId, _context.BranchId));
            if (settingsResult.IsSuccess)
                _currencySymbol = string.IsNullOrWhiteSpace(settingsResult.Value.CurrencySymbol)
                    ? "RD$"
                    : settingsResult.Value.CurrencySymbol;

            var dashboardResult = await DashboardHandler.HandleAsync(new GetManagementDashboardQuery(
                _context.BusinessId,
                _context.BranchId,
                Clock.TodayUtc));

            if (dashboardResult.IsFailure)
                _error = dashboardResult.Error.Description;
            else
                _dashboard = dashboardResult.Value;
        }
        catch
        {
            _error = "No se pudo cargar el estado del dia.";
        }
        finally
        {
            _loading = false;
        }
    }

    private string Money(decimal value) => $"{_currencySymbol} {value:N2}";
    private void GoToPos() => Navigation.NavigateTo("/pos");
    private void GoToCash() => Navigation.NavigateTo("/cash/current");
    private void GoToDailyReport() => Navigation.NavigateTo("/reports/daily");
    private void GoToExpenses() => Navigation.NavigateTo("/expenses");
    private void GoToInventory() => Navigation.NavigateTo("/inventory");
}
