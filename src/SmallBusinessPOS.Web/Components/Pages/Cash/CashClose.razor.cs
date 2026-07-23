using Microsoft.AspNetCore.Components;
using SmallBusinessPOS.Application.Features.CashSessions.CloseCashSession;
using SmallBusinessPOS.Application.Features.CashSessions.DTOs;
using SmallBusinessPOS.Application.Features.CashSessions.GetCashSessionSummary;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Web.Components.Pages.Cash;

public partial class CashClose
{
    [Inject] private GetPosContextHandler PosContextHandler { get; set; } = null!;
    [Inject] private GetCashSessionSummaryHandler SummaryHandler { get; set; } = null!;
    [Inject] private CloseCashSessionHandler CloseHandler { get; set; } = null!;
    [Inject] private ICurrentUserService CurrentUserService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private bool _loading = true;
    private bool _closing;
    private string? _error;
    private string? _success;
    private CashSessionSummaryDto? _summary;
    private CashSessionDto? _closedSession;
    private decimal _countedAmount;
    private string? _notes;
    private decimal CurrentDifference => _closedSession?.Difference ?? (_summary is null ? 0m : _countedAmount - _summary.ExpectedCash);

    protected override async Task OnInitializedAsync()
    {
        await LoadSummaryAsync();
        if (_summary is not null)
            _countedAmount = _summary.ExpectedCash;
    }

    private async Task LoadSummaryAsync()
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

            var summaryResult = await SummaryHandler.HandleAsync(
                new GetCashSessionSummaryQuery(contextResult.Value.CashRegisterId));

            if (summaryResult.IsFailure)
                _error = summaryResult.Error.Description;
            else
                _summary = summaryResult.Value;
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task CloseAsync()
    {
        if (_summary is null || _closing)
            return;

        _closing = true;
        _error = null;
        _success = null;

        try
        {
            var currentUser = await CurrentUserService.GetUserNameAsync();
            var result = await CloseHandler.HandleAsync(
                new CloseCashSessionCommand(_summary.CashSessionId, _countedAmount, _notes),
                currentUser);

            if (result.IsFailure)
            {
                _error = result.Error.Description;
                return;
            }

            _closedSession = result.Value;
            _success = $"Caja cerrada. Diferencia: RD$ {result.Value.Difference?.ToString("N2")}.";
        }
        finally
        {
            _closing = false;
        }
    }

    private void GoToCurrent() => Navigation.NavigateTo("/cash/current");
}
