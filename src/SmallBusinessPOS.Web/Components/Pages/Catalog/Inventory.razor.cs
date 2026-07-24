using Microsoft.AspNetCore.Components;
using SmallBusinessPOS.Application.Features.Inventory.AdjustInventory;
using SmallBusinessPOS.Application.Features.Inventory.GetInventoryMovements;
using SmallBusinessPOS.Application.Features.Inventory.GetInventoryOverview;
using SmallBusinessPOS.Application.Features.Inventory.SetMinimumStock;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Web.Components.Pages.Catalog;

public partial class Inventory
{
    [Inject] private GetPosContextHandler PosContextHandler { get; set; } = null!;
    [Inject] private GetInventoryOverviewHandler OverviewHandler { get; set; } = null!;
    [Inject] private GetInventoryMovementsHandler MovementsHandler { get; set; } = null!;
    [Inject] private AdjustInventoryHandler AdjustHandler { get; set; } = null!;
    [Inject] private SetMinimumStockHandler MinimumHandler { get; set; } = null!;
    [Inject] private ICurrentUserService CurrentUser { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private bool _loading = true;
    private bool _saving;
    private bool _movementsLoading;
    private string? _error;
    private string? _success;
    private Guid _businessId;
    private Guid _branchId;
    private string? _searchTerm;
    private bool _lowStockOnly;
    private int _totalCount;
    private int _lowStockCount;
    private int _outOfStockCount;
    private decimal _totalUnits;
    private const int TablePageSize = 5;
    private int _page = 1;
    private IReadOnlyList<InventoryItemDto> _items = [];
    private IReadOnlyList<InventoryMovementDto> _movements = [];
    private InventoryItemDto? _selected;
    private InventoryItemDto? _adjustItem;
    private InventoryItemDto? _minimumItem;
    private decimal _adjustQuantity;
    private bool _adjustIsDecrease;
    private string _adjustReason = string.Empty;
    private decimal _minimumQuantity;

    private int TotalProducts => _totalCount;
    private int LowStockCount => _lowStockCount;
    private int OutOfStockCount => _outOfStockCount;
    private decimal TotalUnits => _totalUnits;
    private IEnumerable<InventoryItemDto> PagedItems => _items;
    private int TotalPages => Math.Max(1, (int)Math.Ceiling(_totalCount / (double)TablePageSize));
    private string InventoryCountText
    {
        get
        {
            if (_totalCount == 0)
                return "0 productos";

            var start = ((_page - 1) * TablePageSize) + 1;
            var end = Math.Min(_page * TablePageSize, _totalCount);
            return $"Mostrando {start:N0}-{end:N0} de {_totalCount:N0}";
        }
    }
    private string AdjustmentDirectionText => _adjustIsDecrease
        ? "Marcado: esta cantidad se restara del inventario."
        : "Sin marcar: esta cantidad se agregara al inventario.";

    protected override async Task OnInitializedAsync()
    {
        _loading = true;

        try
        {
            var context = await PosContextHandler.HandleAsync(new GetPosContextQuery());
            if (context.IsFailure)
            {
                _error = context.Error.Description;
                return;
            }

            _businessId = context.Value.BusinessId;
            _branchId = context.Value.BranchId;
            await LoadInventoryAsync();
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task LoadInventoryAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            var result = await OverviewHandler.HandleAsync(new GetInventoryOverviewQuery(
                _businessId,
                _branchId,
                _lowStockOnly,
                _searchTerm,
                _page,
                TablePageSize));

            if (result.IsFailure)
            {
                _error = result.Error.Description;
                return;
            }

            _totalCount = result.Value.TotalCount;
            _lowStockCount = result.Value.LowStockCount;
            _outOfStockCount = result.Value.OutOfStockCount;
            _totalUnits = result.Value.TotalUnits;
            var page = Math.Clamp(_page, 1, TotalPages);
            if (page != _page)
            {
                _page = page;
                await LoadInventoryAsync();
                return;
            }

            _items = result.Value.Items;

            if (_selected is not null)
                _selected = _items.FirstOrDefault(i => i.ProductId == _selected.ProductId);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task SetLowStockFilterAsync(bool enabled)
    {
        _lowStockOnly = enabled;
        _page = 1;
        await LoadInventoryAsync();
    }

    private void SetPage(int page) => _page = Math.Clamp(page, 1, TotalPages);

    private async Task SelectProductAsync(InventoryItemDto item)
    {
        _selected = item;
        _movementsLoading = true;
        _movements = [];

        try
        {
            var result = await MovementsHandler.HandleAsync(new GetInventoryMovementsQuery(_businessId, _branchId, item.ProductId));
            if (result.IsFailure)
                _error = result.Error.Description;
            else
                _movements = result.Value;
        }
        finally
        {
            _movementsLoading = false;
        }
    }

    private void OpenAdjust(InventoryItemDto item)
    {
        _adjustItem = item;
        _adjustQuantity = 0m;
        _adjustIsDecrease = false;
        _adjustReason = string.Empty;
        _error = null;
        _success = null;
    }

    private void OpenMinimum(InventoryItemDto item)
    {
        _minimumItem = item;
        _minimumQuantity = item.MinimumQuantity;
        _error = null;
        _success = null;
    }

    private async Task SaveAdjustmentAsync()
    {
        if (_adjustItem is null)
            return;

        if (_saving)
            return;

        _saving = true;
        _error = null;
        _success = null;

        try
        {
            var adjustedProductId = _adjustItem.ProductId;
            var quantity = Math.Abs(_adjustQuantity);
            var signedQuantity = _adjustIsDecrease ? -quantity : quantity;
            var reason = string.IsNullOrWhiteSpace(_adjustReason)
                ? (_adjustIsDecrease ? "Salida manual de inventario" : "Entrada manual de inventario")
                : _adjustReason.Trim();
            var currentUser = await GetCurrentUserAsync();
            var result = await AdjustHandler.HandleAsync(
                new AdjustInventoryCommand(_businessId, _branchId, _adjustItem.ProductId, signedQuantity, reason),
                currentUser);

            if (result.IsFailure)
            {
                _error = result.Error.Description;
                return;
            }

            _success = $"Inventario ajustado. Nueva existencia: {result.Value.NewQuantity:N2}.";
            CloseForms();
            await LoadInventoryAsync();
            if (_selected?.ProductId == adjustedProductId)
                await SelectProductAsync(_selected);
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task SaveMinimumAsync()
    {
        if (_minimumItem is null)
            return;

        if (_saving)
            return;

        _saving = true;
        _error = null;
        _success = null;

        try
        {
            var currentUser = await GetCurrentUserAsync();
            var result = await MinimumHandler.HandleAsync(
                new SetMinimumStockCommand(_businessId, _branchId, _minimumItem.ProductId, _minimumQuantity),
                currentUser);

            if (result.IsFailure)
            {
                _error = result.Error.Description;
                return;
            }

            _success = "Stock minimo actualizado.";
            CloseForms();
            await LoadInventoryAsync();
        }
        finally
        {
            _saving = false;
        }
    }

    private void CloseForms()
    {
        _adjustItem = null;
        _minimumItem = null;
    }

    private void GoToProducts() => Navigation.NavigateTo("/products");

    private async Task<string?> GetCurrentUserAsync()
    {
        return await CurrentUser.GetUserNameAsync();
    }

    private static string Initials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return "PR";

        return string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0])));
    }

    private static string GetStatusText(InventoryItemDto item)
    {
        if (item.Quantity <= 0)
            return "Agotado";

        return item.IsLowStock ? "Bajo Stock" : "Disponible";
    }

    private static string GetStatusClass(InventoryItemDto item)
    {
        if (item.Quantity <= 0)
            return "out";

        return item.IsLowStock ? "low" : "ok";
    }

    private static string GetMovementName(MovementType type) => type switch
    {
        MovementType.Purchase => "Compra",
        MovementType.Sale => "Venta",
        MovementType.SaleCancellation => "Anulacion",
        MovementType.AdjustmentIncrease => "Ajuste +",
        MovementType.AdjustmentDecrease => "Ajuste -",
        MovementType.Waste => "Merma",
        MovementType.Return => "Devolucion",
        MovementType.ProductionInput => "Insumo produccion",
        MovementType.ProductionOutput => "Produccion salida",
        MovementType.InternalUse => "Uso interno",
        MovementType.InitialStock => "Stock inicial",
        _ => type.ToString()
    };
}
