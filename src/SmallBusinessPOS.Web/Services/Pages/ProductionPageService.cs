using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.Production.CancelProductionEntry;
using SmallBusinessPOS.Application.Features.Production.ConfirmProductionEntry;
using SmallBusinessPOS.Application.Features.Production.GetProductionHistory;
using SmallBusinessPOS.Application.Features.Production.GetProductionInputProducts;
using SmallBusinessPOS.Application.Features.Production.GetProductionProducts;
using SmallBusinessPOS.Application.Features.Production.GetProductionRecipe;
using SmallBusinessPOS.Application.Features.Production.SaveProductionRecipe;

namespace SmallBusinessPOS.Web.Services.Pages;

public sealed class ProductionPageService(
    GetPosContextHandler posContextHandler,
    GetProductionProductsHandler productionProductsHandler,
    ConfirmProductionEntryHandler confirmProductionEntryHandler,
    GetProductionHistoryHandler historyHandler,
    GetProductionInputProductsHandler productionInputProductsHandler,
    CancelProductionEntryHandler cancelHandler,
    GetProductionRecipeHandler recipeHandler,
    SaveProductionRecipeHandler saveRecipeHandler,
    AuthenticationStateProvider authenticationStateProvider)
{
    private PosContextDto? _context;

    public List<ProductionProductDto> Products { get; private set; } = [];
    public List<ProductionInputProductDto> InputProducts { get; private set; } = [];
    public List<ProductionLineForm> Lines { get; private set; } = [new()];
    public List<ProductionInputLineForm> InputLines { get; private set; } = [];
    public IReadOnlyList<ProductionHistoryDto> History { get; private set; } = [];
    public string? Error { get; private set; }
    public string? Success { get; private set; }
    public bool Saving { get; private set; }
    public DateTime ProductionDate { get; set; } = DateTime.Today;
    public DateTime HistoryFrom { get; set; } = DateTime.Today.AddDays(-7);
    public DateTime HistoryTo { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
    public ProductionHistoryDto? CancelEntry { get; private set; }
    public ProductionProductDto? RecipeProduct { get; private set; }
    public string CancelReason { get; set; } = string.Empty;
    public List<RecipeLineForm> RecipeLines { get; private set; } = [];

    public decimal FormProduced => Lines.Sum(line => line.QuantityProduced);
    public decimal FormWaste => Lines.Sum(line => line.QuantityWasted);
    public decimal FormNet => Lines.Sum(Net);
    public decimal FormDirectCost => Lines.Sum(DirectCost);
    public decimal HistoryProduced => History.Sum(item => item.TotalProduced);
    public decimal HistoryWaste => History.Sum(item => item.TotalWasted);
    public decimal HistoryNet => History.Sum(item => item.NetAdded);
    public decimal HistoryCost => History.Sum(item => item.TotalCost);

    public async Task InitializeAsync()
    {
        var contextResult = await posContextHandler.HandleAsync(new GetPosContextQuery());
        if (contextResult.IsFailure)
        {
            Error = contextResult.Error.Description;
            return;
        }

        _context = contextResult.Value;

        var productsResult = await productionProductsHandler.HandleAsync(
            new GetProductionProductsQuery(_context.BusinessId));

        if (productsResult.IsFailure)
        {
            Error = productsResult.Error.Description;
            return;
        }

        Products = productsResult.Value;

        var inputProductsResult = await productionInputProductsHandler.HandleAsync(
            new GetProductionInputProductsQuery(_context.BusinessId));

        if (inputProductsResult.IsFailure)
        {
            Error = inputProductsResult.Error.Description;
            return;
        }

        InputProducts = inputProductsResult.Value;
        ResetFirstLineDefaults();

        await LoadHistoryAsync();
    }

    public void AddLine()
    {
        var line = new ProductionLineForm();
        ApplyDefaultProduct(line);
        Lines.Add(line);
    }

    public void RemoveLine(ProductionLineForm line) => Lines.Remove(line);

    public void AddInput()
    {
        var input = new ProductionInputLineForm();
        if (InputProducts.Count > 0)
            input.ProductId = InputProducts[0].ProductId;

        InputLines.Add(input);
    }

    public void RemoveInput(ProductionInputLineForm input) => InputLines.Remove(input);

    public async Task OpenRecipeAsync(Guid productId)
    {
        if (_context is null)
            return;

        ClearMessages();
        RecipeProduct = Products.FirstOrDefault(product => product.ProductId == productId);
        RecipeLines = [];

        if (RecipeProduct is null)
            return;

        var result = await recipeHandler.HandleAsync(new GetProductionRecipeQuery(_context.BusinessId, productId));
        if (result.IsFailure)
        {
            Error = result.Error.Description;
            RecipeProduct = null;
            return;
        }

        RecipeLines = result.Value.Components
            .Select(component => new RecipeLineForm { ProductId = component.ProductId, Quantity = component.Quantity })
            .ToList();
    }

    public void AddRecipeLine()
    {
        var line = new RecipeLineForm();
        var first = InputProducts.FirstOrDefault(product => product.ProductId != RecipeProduct?.ProductId);
        if (first is not null)
            line.ProductId = first.ProductId;

        RecipeLines.Add(line);
    }

    public void RemoveRecipeLine(RecipeLineForm line) => RecipeLines.Remove(line);

    public void CloseRecipe()
    {
        RecipeProduct = null;
        RecipeLines = [];
    }

    public async Task SaveRecipeAsync()
    {
        if (_context is null || RecipeProduct is null)
            return;

        Saving = true;
        ClearMessages();

        var result = await saveRecipeHandler.HandleAsync(new SaveProductionRecipeCommand(
            _context.BusinessId,
            RecipeProduct.ProductId,
            RecipeLines.Select(line => new SaveProductionRecipeComponent(line.ProductId, line.Quantity)).ToList()));

        if (result.IsFailure)
        {
            Error = result.Error.Description;
            Saving = false;
            return;
        }

        Success = $"Receta de {RecipeProduct.Code} actualizada.";
        CloseRecipe();
        Saving = false;
    }

    public void ProductChanged(ProductionLineForm line)
    {
        var product = FindProductionProduct(line.ProductId);
        if (product is not null)
            line.UnitCost = product.EstimatedCost;
    }

    public async Task LoadHistoryAsync()
    {
        if (_context is null)
            return;

        var result = await historyHandler.HandleAsync(new GetProductionHistoryQuery(
            _context.BusinessId,
            _context.BranchId,
            DateOnly.FromDateTime(HistoryFrom.Date),
            DateOnly.FromDateTime(HistoryTo.Date)));

        if (result.IsFailure)
            Error = result.Error.Description;
        else
            History = result.Value;
    }

    public async Task ConfirmAsync()
    {
        Saving = true;
        ClearMessages();

        if (_context is null)
        {
            Error = "No hay contexto operativo disponible.";
            Saving = false;
            return;
        }

        var result = await confirmProductionEntryHandler.HandleAsync(new ConfirmProductionEntryCommand(
            ProductionEntryId: null,
            _context.BusinessId,
            _context.BranchId,
            DateOnly.FromDateTime(ProductionDate.Date),
            Lines.Select(line => new ConfirmProductionEntryLine(line.ProductId, line.QuantityProduced, line.UnitCost, line.QuantityWasted)).ToList(),
            Notes,
            Inputs: InputLines.Select(input => new ConfirmProductionInputLine(input.ProductId, input.Quantity)).ToList()), await GetCurrentUserAsync());

        if (result.IsFailure)
        {
            Error = result.Error.Description;
            Saving = false;
            return;
        }

        Success = $"Produccion {result.Value.Number} confirmada. Producido: {result.Value.TotalQuantityProduced:N2}. Merma: {result.Value.TotalQuantityWasted:N2}. Agregado neto: {result.Value.NetQuantityAdded:N2}.";
        ResetForm();
        await LoadHistoryAsync();
        Saving = false;
    }

    public void OpenCancel(ProductionHistoryDto entry)
    {
        CancelEntry = entry;
        CancelReason = string.Empty;
        ClearMessages();
    }

    public void CloseCancel()
    {
        CancelEntry = null;
        CancelReason = string.Empty;
    }

    public async Task CancelAsync()
    {
        if (CancelEntry is null)
            return;

        Saving = true;
        ClearMessages();

        var result = await cancelHandler.HandleAsync(
            new CancelProductionEntryCommand(CancelEntry.ProductionEntryId, CancelReason),
            await GetCurrentUserAsync());

        if (result.IsFailure)
            Error = result.Error.Description;
        else
            Success = $"Produccion {CancelEntry.Number} anulada y reversada.";

        CloseCancel();
        await LoadHistoryAsync();
        Saving = false;
    }

    public ProductionProductDto? FindProductionProduct(Guid productId) =>
        Products.FirstOrDefault(product => product.ProductId == productId);

    public static decimal Net(ProductionLineForm line) =>
        Math.Max(0m, line.QuantityProduced - line.QuantityWasted);

    public static decimal DirectCost(ProductionLineForm line) =>
        line.QuantityProduced * line.UnitCost;

    public static decimal ExpectedMarginPercent(ProductionLineForm line, ProductionProductDto? product)
    {
        if (product is null || product.SalePrice <= 0m || line.QuantityProduced <= 0m)
            return 0m;

        var sales = Net(line) * product.SalePrice;
        if (sales <= 0m)
            return 0m;

        return ((sales - DirectCost(line)) / sales) * 100m;
    }

    public static string GetStatusName(string status) => status switch
    {
        "Draft" => "Borrador",
        "Confirmed" => "Confirmada",
        "Cancelled" => "Anulada",
        _ => status
    };

    public static string GetStatusClass(string status) => status switch
    {
        "Confirmed" => "bg-success-subtle text-success",
        "Cancelled" => "bg-danger-subtle text-danger",
        _ => "bg-secondary-subtle text-secondary"
    };

    private void ResetForm()
    {
        Lines = [new()];
        InputLines = [];
        ResetFirstLineDefaults();
        Notes = null;
    }

    private void ResetFirstLineDefaults()
    {
        if (Lines.Count == 0)
            Lines.Add(new ProductionLineForm());

        ApplyDefaultProduct(Lines[0]);
    }

    private void ApplyDefaultProduct(ProductionLineForm line)
    {
        if (Products.Count > 0)
        {
            line.ProductId = Products[0].ProductId;
            line.UnitCost = Products[0].EstimatedCost;
        }
    }

    private void ClearMessages()
    {
        Error = null;
        Success = null;
    }

    private async Task<string?> GetCurrentUserAsync()
    {
        var auth = await authenticationStateProvider.GetAuthenticationStateAsync();
        return auth.User.Identity?.Name
            ?? auth.User.FindFirstValue(ClaimTypes.Email)
            ?? auth.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}

public sealed class ProductionLineForm
{
    public Guid ProductId { get; set; }
    public decimal QuantityProduced { get; set; }
    public decimal QuantityWasted { get; set; }
    public decimal UnitCost { get; set; }
}

public sealed class ProductionInputLineForm
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
}

public sealed class RecipeLineForm
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
}
