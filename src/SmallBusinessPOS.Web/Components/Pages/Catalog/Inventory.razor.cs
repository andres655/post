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
    private bool _loading = true;
    private const int PageSize = 200;
    private bool _saving;
    private bool _movementsLoading;
    private string? _error;
    private string? _success;
    private Guid _businessId;
    private Guid _branchId;
    private string? _searchTerm;
    private bool _lowStockOnly;
    private int _lowStockCount;
    private const int TablePageSize = 10;
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

    private int TotalProducts => _items.Count;
    private int LowStockCount => _lowStockCount;
    private int OutOfStockCount => _items.Count(i => i.Quantity <= 0);
    private decimal TotalUnits => _items.Sum(i => i.Quantity);
    private IEnumerable<InventoryItemDto> PagedItems => _items.Skip((_page - 1) * TablePageSize).Take(TablePageSize);
    private int TotalPages => Math.Max(1, (int)Math.Ceiling(_items.Count / (double)TablePageSize));
    private string InventoryCountText
    {
        get
        {
            if (_items.Count == 0)
                return "0 productos";

            var start = ((_page - 1) * TablePageSize) + 1;
            var end = Math.Min(_page * TablePageSize, _items.Count);
            return $"Mostrando {start:N0}-{end:N0} de {_items.Count:N0}";
        }
    }
    private string AdjustmentDirectionText => _adjustIsDecrease
        ? "Marcado: esta cantidad se restara del inventario."
        : "Sin marcar: esta cantidad se agregara al inventario.";

    protected override async Task OnInitializedAsync()
    {
        var context = await PosContextHandler.HandleAsync(new GetPosContextQuery());
        if (context.IsFailure)
        {
            _error = context.Error.Description;
            _loading = false;
            return;
        }

        _businessId = context.Value.BusinessId;
        _branchId = context.Value.BranchId;
        await LoadInventoryAsync();
    }

    private async Task LoadInventoryAsync()
    {
        _loading = true;
        _error = null;

        var result = await OverviewHandler.HandleAsync(new GetInventoryOverviewQuery(
            _businessId,
            _branchId,
            _lowStockOnly,
            _searchTerm,
            PageSize));

        if (result.IsFailure)
        {
            _error = result.Error.Description;
        }
        else
        {
            _items = result.Value;
            _page = Math.Min(_page, TotalPages);
            _lowStockCount = _items.Count(i => i.IsLowStock);
            if (_selected is not null)
                _selected = _items.FirstOrDefault(i => i.ProductId == _selected.ProductId);
        }

        _loading = false;
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

        var result = await MovementsHandler.HandleAsync(new GetInventoryMovementsQuery(_businessId, _branchId, item.ProductId));
        if (result.IsFailure)
            _error = result.Error.Description;
        else
            _movements = result.Value;

        _movementsLoading = false;
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

        _saving = true;
        _error = null;
        _success = null;

        var adjustedProductId = _adjustItem.ProductId;
        var quantity = Math.Abs(_adjustQuantity);
        if (quantity <= 0m)
        {
            _error = "Indica una cantidad mayor que cero para ajustar el inventario.";
            _saving = false;
            return;
        }

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
            _saving = false;
            return;
        }

        _success = $"Inventario ajustado. Nueva existencia: {result.Value.NewQuantity:N2}.";
        CloseForms();
        await LoadInventoryAsync();
        if (_selected?.ProductId == adjustedProductId)
            await SelectProductAsync(_selected);
        _saving = false;
    }

    private async Task SaveMinimumAsync()
    {
        if (_minimumItem is null)
            return;

        _saving = true;
        var currentUser = await GetCurrentUserAsync();
        var result = await MinimumHandler.HandleAsync(
            new SetMinimumStockCommand(_businessId, _branchId, _minimumItem.ProductId, _minimumQuantity),
            currentUser);

        if (result.IsFailure)
            _error = result.Error.Description;
        else
            _success = "Stock minimo actualizado.";

        CloseForms();
        await LoadInventoryAsync();
        _saving = false;
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
