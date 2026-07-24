using Microsoft.AspNetCore.Components;
using SmallBusinessPOS.Application.Features.Categories.CreateCategory;
using SmallBusinessPOS.Application.Features.Categories.DisableCategory;
using SmallBusinessPOS.Application.Features.Categories.DTOs;
using SmallBusinessPOS.Application.Features.Categories.GetCategories;
using SmallBusinessPOS.Application.Features.Categories.UpdateCategory;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;

namespace SmallBusinessPOS.Web.Components.Pages.Catalog;

public partial class Categories
{
    [Inject] private CreateCategoryHandler CreateHandler { get; set; } = null!;
    [Inject] private UpdateCategoryHandler UpdateHandler { get; set; } = null!;
    [Inject] private GetCategoriesHandler GetHandler { get; set; } = null!;
    [Inject] private DisableCategoryHandler DisableHandler { get; set; } = null!;
    [Inject] private GetPosContextHandler PosContextHandler { get; set; } = null!;

    private Guid _businessId = Guid.Empty;
    private List<CategoryDto>? _categories;
    private const int TablePageSize = 5;
    private int _page = 1;
    private bool _loading = true;
    private string? _errorMessage;
    private string? _successMessage;

    private bool _mostrarFormulario;
    private bool _editando;
    private Guid _editandoId;
    private string _formNombre = string.Empty;
    private string? _formDescripcion;
    private int _formOrden;
    private string? _formError;
    private bool _saving;

    private CategoryDto? _categoriaADeshabilitar;

    private IEnumerable<CategoryDto> PagedCategories =>
        (_categories ?? []).Skip((_page - 1) * TablePageSize).Take(TablePageSize);

    private int TotalPages => Math.Max(1, (int)Math.Ceiling((_categories?.Count ?? 0) / (double)TablePageSize));

    private string CategoriesCountText
    {
        get
        {
            var total = _categories?.Count ?? 0;
            if (total == 0)
                return "0 categorias";

            var start = ((_page - 1) * TablePageSize) + 1;
            var end = Math.Min(_page * TablePageSize, total);
            return $"Mostrando {start:N0}-{end:N0} de {total:N0}";
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await CargarBusinessIdAsync();
        await CargarCategoriasAsync();
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

    private async Task CargarCategoriasAsync()
    {
        _loading = true;

        try
        {
            if (_businessId == Guid.Empty)
            {
                _errorMessage = "No se pudo determinar el negocio activo.";
                return;
            }

            var result = await GetHandler.HandleAsync(new GetCategoriesQuery(_businessId, OnlyActive: false));
            if (result.IsSuccess)
            {
                _categories = result.Value;
                _page = Math.Clamp(_page, 1, TotalPages);
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

    private void SetPage(int page) => _page = Math.Clamp(page, 1, TotalPages);

    private void NuevaCategoria()
    {
        _editando = false;
        _editandoId = Guid.Empty;
        _formNombre = string.Empty;
        _formDescripcion = null;
        _formOrden = _categories?.Count ?? 0;
        _formError = null;
        _mostrarFormulario = true;
    }

    private void EditarCategoria(CategoryDto cat)
    {
        _editando = true;
        _editandoId = cat.Id;
        _formNombre = cat.Name;
        _formDescripcion = cat.Description;
        _formOrden = cat.SortOrder;
        _formError = null;
        _mostrarFormulario = true;
    }

    private async Task GuardarCategoria()
    {
        if (_saving)
            return;

        _saving = true;
        _formError = null;

        try
        {
            var result = _editando
                ? await UpdateHandler.HandleAsync(new UpdateCategoryCommand(_editandoId, _formNombre, _formDescripcion, _formOrden))
                : await CreateHandler.HandleAsync(new CreateCategoryCommand(_businessId, _formNombre, _formDescripcion, _formOrden));

            if (result.IsSuccess)
            {
                _successMessage = _editando
                    ? "Categoria actualizada correctamente."
                    : "Categoria creada correctamente.";
                CerrarFormulario();
                await CargarCategoriasAsync();
            }
            else
            {
                _formError = result.Error.Description;
            }
        }
        finally
        {
            _saving = false;
        }
    }

    private void CerrarFormulario()
    {
        _mostrarFormulario = false;
        _formError = null;
    }

    private void ConfirmarDeshabilitar(CategoryDto cat) => _categoriaADeshabilitar = cat;

    private static string Initials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "C";

        var parts = value
            .Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0]));

        return string.Concat(parts);
    }

    private async Task DeshabilitarCategoria()
    {
        if (_categoriaADeshabilitar is null || _saving)
            return;

        _saving = true;
        _errorMessage = null;

        try
        {
            var category = _categoriaADeshabilitar;
            var result = await DisableHandler.HandleAsync(new DisableCategoryCommand(category.Id));
            if (result.IsSuccess)
            {
                _successMessage = $"Categoria '{category.Name}' deshabilitada.";
                _categoriaADeshabilitar = null;
                await CargarCategoriasAsync();
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
}
