using Microsoft.AspNetCore.Components;
using SmallBusinessPOS.Application.Features.Categories.DTOs;
using SmallBusinessPOS.Application.Features.Categories.GetCategories;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.Products.CreateProduct;
using SmallBusinessPOS.Application.Features.Products.DisableProduct;
using SmallBusinessPOS.Application.Features.Products.DTOs;
using SmallBusinessPOS.Application.Features.Products.GetProduct;
using SmallBusinessPOS.Application.Features.Products.GetProducts;
using SmallBusinessPOS.Application.Features.Products.UpdateProduct;
using SmallBusinessPOS.Application.Features.ProductTypes.DTOs;
using SmallBusinessPOS.Application.Features.ProductTypes.GetProductTypes;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Web.Components.Pages.Catalog;

public partial class Products
{
    [Inject] private CreateProductHandler CreateHandler { get; set; } = null!;
    [Inject] private UpdateProductHandler UpdateHandler { get; set; } = null!;
    [Inject] private GetProductsHandler GetHandler { get; set; } = null!;
    [Inject] private GetProductHandler GetProductHandler { get; set; } = null!;
    [Inject] private GetCategoriesHandler GetCategoriesHandler { get; set; } = null!;
    [Inject] private DisableProductHandler DisableHandler { get; set; } = null!;
    [Inject] private GetPosContextHandler PosContextHandler { get; set; } = null!;
    [Inject] private GetProductTypesHandler GetProductTypesHandler { get; set; } = null!;

    private Guid _businessId = Guid.Empty;
    private const int TablePageSize = 5;

    private IReadOnlyList<ProductSummaryDto>? _products;
    private List<CategoryDto>? _categories;
    private List<ProductTypeOptionDto> _productTypes = [];
    private IReadOnlyList<ProductSummaryDto> _inventoryBaseProducts = [];
    private int _totalCount;

    private bool _loading = true;
    private string? _errorMessage;
    private string? _successMessage;

    private string _searchTerm = string.Empty;
    private string _filtroCategoria = string.Empty;
    private string _filtroTipo = string.Empty;
    private bool _soloActivos = true;
    private int _page = 1;

    private bool _mostrarFormulario;
    private bool _editando;
    private Guid _editandoId;
    private ProductForm _form = new();
    private string? _formError;
    private bool _saving;

    private ProductSummaryDto? _productoADeshabilitar;
    private IEnumerable<ProductSummaryDto> InventorySourceOptions =>
        _inventoryBaseProducts.Where(p => !_editando || p.Id != _editandoId);
    private bool IsComboProduct => (ProductType)_form.ProductTypeInt == ProductType.Combo;
    private bool IsPortionProduct => (UnitOfMeasure)_form.UnitOfMeasureInt == UnitOfMeasure.Portion;
    private IReadOnlyList<ProductTypeOptionDto> ProductTypeOptions =>
        _productTypes;
    private static int UnitValue => (int)UnitOfMeasure.Unit;
    private static int PortionValue => (int)UnitOfMeasure.Portion;
    private static int PoundValue => (int)UnitOfMeasure.Pound;
    private static int KilogramValue => (int)UnitOfMeasure.Kilogram;
    private static int LiterValue => (int)UnitOfMeasure.Liter;
    private static int GramValue => (int)UnitOfMeasure.Gram;
    private IEnumerable<ProductSummaryDto> PagedProducts => _products ?? [];
    private int TotalPages => Math.Max(1, (int)Math.Ceiling(_totalCount / (double)TablePageSize));
    private string ProductsCountText
    {
        get
        {
            var total = _totalCount;
            if (total == 0)
                return "0 productos";

            var start = ((_page - 1) * TablePageSize) + 1;
            var end = Math.Min(_page * TablePageSize, total);
            return $"Mostrando {start:N0}-{end:N0} de {total:N0}";
        }
    }

    protected override async Task OnInitializedAsync()
    {
        _loading = true;

        try
        {
            await CargarBusinessIdAsync();
            await CargarTiposProductoAsync();
            await CargarProductosAsync();
            await CargarProductosBaseAsync();
            await CargarCategoriasAsync();
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task CargarBusinessIdAsync()
    {
        var contextResult = await PosContextHandler.HandleAsync(new GetPosContextQuery());
        if (contextResult.IsSuccess)
        {
            _businessId = contextResult.Value.BusinessId;
            return;
        }

        _errorMessage = contextResult.Error.Description;
    }

    private async Task CargarProductosAsync()
    {
        _loading = true;
        _errorMessage = null;

        try
        {
            if (_businessId == Guid.Empty)
                return;

            Guid? catId = Guid.TryParse(_filtroCategoria, out var cid) ? cid : null;
            ProductType? tipo = int.TryParse(_filtroTipo, out var ti) ? (ProductType)ti : null;

            var result = await GetHandler.HandleAsync(new GetProductsQuery(
                _businessId, _soloActivos, catId, tipo,
                string.IsNullOrWhiteSpace(_searchTerm) ? null : _searchTerm,
                _page,
                TablePageSize));

            if (result.IsSuccess)
            {
                _totalCount = result.Value.TotalCount;
                var page = Math.Clamp(_page, 1, TotalPages);
                if (page != _page)
                {
                    _page = page;
                    await CargarProductosAsync();
                    return;
                }

                _products = result.Value.Items;
            }
            else
            {
                _errorMessage = result.Error.Description;
            }
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task CargarProductosBaseAsync()
    {
        if (_businessId == Guid.Empty)
            return;

        var result = await GetHandler.HandleAsync(new GetProductsQuery(
            _businessId,
            OnlyActive: true,
            PageSize: 500));

        if (result.IsSuccess)
            _inventoryBaseProducts = result.Value.Items.Where(p => p.TracksInventory).ToList();
    }

    private async Task CargarCategoriasAsync()
    {
        if (_businessId == Guid.Empty) return;
        var result = await GetCategoriesHandler.HandleAsync(new GetCategoriesQuery(_businessId, MaxRows: 200));
        if (result.IsSuccess) _categories = result.Value;
    }

    private async Task CargarTiposProductoAsync()
    {
        if (_businessId == Guid.Empty)
            return;

        var result = await GetProductTypesHandler.HandleAsync(new GetProductTypesQuery(_businessId));
        if (result.IsSuccess)
            _productTypes = result.Value;
    }

    private async Task FiltrarProductos()
    {
        _page = 1;
        await CargarProductosAsync();
    }

    private void SetPage(int page) => _page = Math.Clamp(page, 1, TotalPages);

    private void NuevoProducto()
    {
        _editando = false;
        _editandoId = Guid.Empty;
        _form = new ProductForm();
        _formError = null;
        _mostrarFormulario = true;
    }

    private async Task EditarProducto(Guid id)
    {
        var result = await GetProductHandler.HandleAsync(new GetProductQuery(id));
        if (result.IsSuccess)
        {
            var p = result.Value;
            _editando = true;
            _editandoId = id;
            _form = new ProductForm
            {
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                Barcode = p.Barcode,
                ProductTypeInt = (int)p.ProductType,
                UnitOfMeasureInt = (int)p.UnitOfMeasure,
                SalePrice = p.SalePrice,
                EstimatedCost = p.EstimatedCost,
                CategoryIdStr = p.CategoryId?.ToString(),
                TracksInventory = p.TracksInventory,
                AllowsFractionalQuantity = p.AllowsFractionalQuantity,
                InventorySourceProductIdStr = p.InventorySourceProductId?.ToString(),
                InventorySourceQuantity = p.InventorySourceQuantity ?? 0m,
                HadInventorySource = p.InventoryComponents.Count > 0,
                Components = p.InventoryComponents
                    .Select(c => new ProductComponentForm
                    {
                        ProductIdStr = c.ProductId.ToString(),
                        Quantity = c.Quantity
                    })
                    .ToList()
            };
            _formError = null;
            _mostrarFormulario = true;
        }
        else
        {
            _errorMessage = result.Error.Description;
        }
    }

    private async Task GuardarProducto()
    {
        if (_saving)
            return;

        _saving = true;
        _formError = null;

        try
        {
            Guid? catId = Guid.TryParse(_form.CategoryIdStr, out var cid) ? cid : null;
            var inventoryComponents = BuildInventoryComponents();
            var clearInventoryComponents = _form.HadInventorySource && inventoryComponents.Count == 0;

            if (_editando)
            {
                var cmd = new UpdateProductCommand(
                    _editandoId, _form.Code, _form.Name,
                    (ProductType)_form.ProductTypeInt, (UnitOfMeasure)_form.UnitOfMeasureInt,
                    _form.SalePrice, _form.EstimatedCost, catId,
                    _form.TracksInventory, _form.AllowsFractionalQuantity,
                    _form.Description, _form.Barcode,
                    ClearInventorySource: clearInventoryComponents,
                    InventoryComponents: inventoryComponents.Count > 0 ? inventoryComponents : null);
                var result = await UpdateHandler.HandleAsync(cmd);
                if (result.IsSuccess)
                {
                    _successMessage = "Producto actualizado.";
                    CerrarFormulario();
                    await CargarProductosAsync();
                    await CargarProductosBaseAsync();
                }
                else
                {
                    _formError = result.Error.Description;
                }
            }
            else
            {
                var cmd = new CreateProductCommand(
                    _businessId, _form.Code, _form.Name,
                    (ProductType)_form.ProductTypeInt, (UnitOfMeasure)_form.UnitOfMeasureInt,
                    _form.SalePrice, _form.EstimatedCost, catId,
                    _form.TracksInventory, _form.AllowsFractionalQuantity,
                    _form.Description, _form.Barcode,
                    InventoryComponents: inventoryComponents.Count > 0 ? inventoryComponents : null);
                var result = await CreateHandler.HandleAsync(cmd);
                if (result.IsSuccess)
                {
                    _successMessage = "Producto creado.";
                    CerrarFormulario();
                    await CargarProductosAsync();
                    await CargarProductosBaseAsync();
                }
                else
                {
                    _formError = result.Error.Description;
                }
            }
        }
        finally
        {
            _saving = false;
        }
    }

    private List<ProductInventoryComponentInput> BuildInventoryComponents()
    {
        if (IsPortionProduct)
        {
            var sourceProductId = Guid.TryParse(_form.InventorySourceProductIdStr, out var sourceId) ? sourceId : Guid.Empty;
            if (sourceProductId == Guid.Empty)
                return [];

            return [new ProductInventoryComponentInput(sourceProductId, _form.InventorySourceQuantity)];
        }

        if (!IsComboProduct)
            return [];

        var components = new List<ProductInventoryComponentInput>();
        foreach (var component in _form.Components)
        {
            var productId = Guid.TryParse(component.ProductIdStr, out var parsedId) ? parsedId : Guid.Empty;
            components.Add(new ProductInventoryComponentInput(productId, component.Quantity));
        }

        return components;
    }

    private void AgregarComponenteCombo()
    {
        _form.Components.Add(new ProductComponentForm { Quantity = 1m });
    }

    private void QuitarComponenteCombo(ProductComponentForm component)
    {
        _form.Components.Remove(component);
    }

    private void CerrarFormulario() { _mostrarFormulario = false; _formError = null; }

    private async Task DeshabilitarProducto()
    {
        if (_productoADeshabilitar is null || _saving)
            return;

        _saving = true;
        _errorMessage = null;

        try
        {
            var result = await DisableHandler.HandleAsync(new DisableProductCommand(_productoADeshabilitar.Id));
            if (result.IsSuccess)
            {
                _successMessage = "Producto deshabilitado.";
                _productoADeshabilitar = null;
                await CargarProductosAsync();
            }
            else
            {
                _errorMessage = result.Error.Description;
            }
        }
        finally
        {
            _saving = false;
        }
    }

    private static string Initials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "PR";

        var parts = value
            .Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0]));

        return string.Concat(parts);
    }

    private static string GetTypeName(ProductType t) => t switch
    {
        ProductType.Standard => "Estándar",
        ProductType.PreparedItem => "Preparado",
        ProductType.Combo => "Combo",
        ProductType.Service => "Servicio",
        ProductType.Ingredient => "Ingrediente",
        ProductType.Packaging => "Empaque",
        _ => t.ToString()
    };

    private string GetTypeDisplayName(ProductType type) =>
        ProductTypeOptions.FirstOrDefault(option => option.Value == type)?.Name ?? GetTypeName(type);

    private static string GetTypeBadgeClass(ProductType t) => t switch
    {
        ProductType.PreparedItem => "prepared",
        ProductType.Combo => "combo",
        ProductType.Service => "service",
        ProductType.Ingredient => "ingredient",
        ProductType.Packaging => "packaging",
        _ => "standard"
    };

    private sealed class ProductForm
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Barcode { get; set; }
        public int ProductTypeInt { get; set; } = (int)ProductType.Standard;
        public int UnitOfMeasureInt { get; set; } = (int)UnitOfMeasure.Unit;
        public decimal SalePrice { get; set; }
        public decimal EstimatedCost { get; set; }
        public string? CategoryIdStr { get; set; }
        public bool TracksInventory { get; set; } = true;
        public bool AllowsFractionalQuantity { get; set; }
        public string? InventorySourceProductIdStr { get; set; }
        public decimal InventorySourceQuantity { get; set; }
        public bool HadInventorySource { get; set; }
        public List<ProductComponentForm> Components { get; set; } = [];
    }

    private sealed class ProductComponentForm
    {
        public string? ProductIdStr { get; set; }
        public decimal Quantity { get; set; } = 1m;
    }

}
