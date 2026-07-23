using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SmallBusinessPOS.Application.Features.CashSessions.CloseCashSession;
using SmallBusinessPOS.Application.Features.CashSessions.DTOs;
using SmallBusinessPOS.Application.Features.CashSessions.GetCurrentCashSession;
using SmallBusinessPOS.Application.Features.CashSessions.OpenCashSession;
using SmallBusinessPOS.Application.Features.Categories.DTOs;
using SmallBusinessPOS.Application.Features.Categories.GetCategories;
using SmallBusinessPOS.Application.Features.Customers;
using SmallBusinessPOS.Application.Features.Customers.CreateCustomer;
using SmallBusinessPOS.Application.Features.Customers.GetCustomers;
using SmallBusinessPOS.Application.Features.PaymentMethods.GetActivePaymentMethods;
using SmallBusinessPOS.Application.Features.POS.Checkout;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.POS.GetPosOptions;
using SmallBusinessPOS.Application.Features.Products.DTOs;
using SmallBusinessPOS.Application.Features.Products.GetProducts;
using SmallBusinessPOS.Application.Features.Sales.CreateSale;
using SmallBusinessPOS.Application.Features.Settings;
using SmallBusinessPOS.Application.Features.Settings.GetBusinessSettings;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Web.Components.Pages.Pos;

public partial class Pos
{
    private bool _loading = true;
    private string? _error;
    private string? _success;

    private PosContextDto? _context;
    private CashSessionDto? _currentSession;
    private BusinessSettingsDto? _businessSettings;
    private List<PosBranchOptionDto> _branches = [];
    private List<PosCashRegisterOptionDto> _registers = [];
    private List<CustomerDto> _customers = [];
    private string? _selectedBranchId;
    private string? _selectedRegisterId;
    private string? _selectedCustomerId;
    private string _newCustomerName = string.Empty;
    private decimal _openingAmount;
    private bool _showCloseModal;
    private decimal _countedAmount;
    private string? _closeNotes;

    private List<CategoryDto> _categories = [];
    private List<ProductSummaryDto> _products = [];
    private List<ProductSummaryDto> _filteredProducts = [];
    private readonly List<CartLine> _cart = [];
    private readonly List<PaymentLine> _payments = [];
    private const int ProductGridLimit = 120;

    private string _search = string.Empty;
    private string? _selectedCategoryId;

    private decimal _discount;
    private decimal _tax;
    private Guid? _lastSaleId;
    private string CurrencySymbol => string.IsNullOrWhiteSpace(_businessSettings?.CurrencySymbol) ? "RD$" : _businessSettings.CurrencySymbol;
    private decimal Subtotal => CheckoutCalculator.CalculateSubtotal(BuildCartInputs());
    private decimal Total => CheckoutCalculator.CalculateTotal(Subtotal, _discount, _tax);
    private decimal Discount
    {
        get => _discount;
        set
        {
            _discount = CheckoutCalculator.NormalizeDiscount(value);
            RecalculateTax();
        }
    }
    private decimal Paid => CheckoutCalculator.CalculatePaidTotal(BuildPaymentInputs());
    private IEnumerable<PosCashRegisterOptionDto> AvailableRegisters =>
        Guid.TryParse(_selectedBranchId, out var branchId)
            ? _registers.Where(register => register.BranchId == branchId)
            : _registers;
    private IEnumerable<PaymentLine> AllowedPayments =>
        _payments.Where(payment => payment.Type != PaymentMethodType.Credit || (_businessSettings?.AllowsCredit ?? false));
    private string SelectedCustomerName =>
        Guid.TryParse(_selectedCustomerId, out var customerId)
            ? _customers.FirstOrDefault(customer => customer.Id == customerId)?.Name ?? "Consumidor final"
            : "Consumidor final";
    private decimal Change
    {
        get
        {
            return CheckoutCalculator.CalculateCashChange(Total, BuildPaymentInputs());
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
        _success = null;

        var contextResult = await PosContextHandler.HandleAsync(new GetPosContextQuery());
        if (contextResult.IsFailure)
        {
            _error = contextResult.Error.Description;
            _loading = false;
            return;
        }

        _context = contextResult.Value;
        _selectedBranchId = _context.BranchId.ToString();
        _selectedRegisterId = _context.CashRegisterId.ToString();

        var optionsResult = await PosOptionsHandler.HandleAsync(new GetPosOptionsQuery(_context.BusinessId));
        if (optionsResult.IsSuccess)
        {
            _branches = optionsResult.Value.Branches;
            _registers = optionsResult.Value.CashRegisters;
        }

        var settingsResult = await GetSettingsHandler.HandleAsync(
            new GetBusinessSettingsQuery(_context.BusinessId, _context.BranchId));
        if (settingsResult.IsSuccess)
            _businessSettings = settingsResult.Value;

        var categoriesResult = await CategoriesHandler.HandleAsync(new GetCategoriesQuery(_context.BusinessId));
        if (categoriesResult.IsSuccess)
            _categories = categoriesResult.Value;

        var productsResult = await ProductsHandler.HandleAsync(new GetProductsQuery(_context.BusinessId));
        if (productsResult.IsSuccess)
        {
            _products = productsResult.Value;
            _filteredProducts = _products;
        }

        var methodsResult = await PaymentMethodsHandler.HandleAsync(new GetActivePaymentMethodsQuery(_context.BusinessId));
        if (methodsResult.IsSuccess)
        {
            _payments.Clear();
            foreach (var method in methodsResult.Value)
                _payments.Add(new PaymentLine(method.Id, method.Code, method.Name, method.Type));
        }

        await LoadCustomersAsync();
        await RefreshSessionAsync();
        _loading = false;
    }

    private async Task OnStationChangedAsync()
    {
        if (_context is null || !Guid.TryParse(_selectedBranchId, out var branchId))
            return;

        var availableRegisters = _registers.Where(register => register.BranchId == branchId).ToList();
        if (!Guid.TryParse(_selectedRegisterId, out var registerId) || availableRegisters.All(register => register.Id != registerId))
            _selectedRegisterId = availableRegisters.FirstOrDefault()?.Id.ToString();

        if (!Guid.TryParse(_selectedRegisterId, out registerId))
            return;

        var contextResult = await PosContextHandler.HandleAsync(new GetPosContextQuery(
            _context.BusinessId,
            branchId,
            registerId));
        if (contextResult.IsFailure)
        {
            _error = contextResult.Error.Description;
            return;
        }

        _context = contextResult.Value;
        var settingsResult = await GetSettingsHandler.HandleAsync(
            new GetBusinessSettingsQuery(_context.BusinessId, _context.BranchId));
        if (settingsResult.IsSuccess)
            _businessSettings = settingsResult.Value;

        ClearCart();
        await RefreshSessionAsync();
    }

    private async Task LoadCustomersAsync()
    {
        if (_context is null)
            return;

        var result = await CustomersHandler.HandleAsync(new GetCustomersQuery(_context.BusinessId));
        if (result.IsSuccess)
            _customers = result.Value;
    }

    private async Task RefreshSessionAsync()
    {
        if (_context is null)
            return;

        var sessionResult = await CurrentSessionHandler.HandleAsync(new GetCurrentCashSessionQuery(_context.CashRegisterId));
        if (sessionResult.IsSuccess)
            _currentSession = sessionResult.Value;
        else
            _currentSession = null;
    }

    private void SelectCategory(Guid? categoryId)
    {
        _selectedCategoryId = categoryId?.ToString();
        FilterProducts();
    }

    private void FilterProducts()
    {
        IEnumerable<ProductSummaryDto> query = _products;

        if (!string.IsNullOrWhiteSpace(_selectedCategoryId))
        {
            var categoryName = _categories
                .Where(c => c.Id.ToString() == _selectedCategoryId)
                .Select(c => c.Name)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(categoryName))
                query = query.Where(p => p.CategoryName == categoryName);
        }

        if (!string.IsNullOrWhiteSpace(_search))
        {
            var term = _search.Trim().ToLowerInvariant();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term)
                || p.Code.ToLower().Contains(term)
                || (!string.IsNullOrWhiteSpace(p.Barcode) && p.Barcode.ToLower().Contains(term)));
        }

        _filteredProducts = query.Take(ProductGridLimit).ToList();
    }

    private void HandleSearchKeyDown(KeyboardEventArgs args)
    {
        if (args.Key != "Enter" || string.IsNullOrWhiteSpace(_search))
            return;

        var term = _search.Trim();
        var exact = _products.FirstOrDefault(product =>
            product.Code.Equals(term, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(product.Barcode)
                && product.Barcode.Equals(term, StringComparison.OrdinalIgnoreCase)));

        if (exact is null)
            return;

        AddToCart(exact);
        _search = string.Empty;
        FilterProducts();
    }

    private void AddToCart(ProductSummaryDto product)
    {
        var existing = _cart.FirstOrDefault(x => x.ProductId == product.Id);
        if (existing is not null)
        {
            existing.Quantity += 1;
            RecalculateTax();
            return;
        }

        _cart.Add(new CartLine
        {
            ProductId = product.Id,
            Name = product.Name,
            UnitPrice = product.SalePrice,
            Quantity = 1m,
            AllowsFractionalQuantity = product.AllowsFractionalQuantity
        });
        RecalculateTax();
    }

    private void ChangeQty(Guid productId, int delta)
    {
        var item = _cart.FirstOrDefault(x => x.ProductId == productId);
        if (item is null)
            return;

        item.Quantity += delta;
        if (item.Quantity <= 0)
            _cart.Remove(item);
        RecalculateTax();
    }

    private void ClearCart()
    {
        _cart.Clear();
        _discount = 0;
        _tax = 0;
        foreach (var payment in _payments)
        {
            payment.Amount = 0;
            payment.Reference = null;
        }
    }

    private async Task OpenSessionAsync()
    {
        if (_context is null)
            return;

        var result = await OpenSessionHandler.HandleAsync(new OpenCashSessionCommand(
            _context.BusinessId,
            _context.BranchId,
            _context.CashRegisterId,
            _openingAmount), await GetCurrentUserAsync());

        if (result.IsFailure)
        {
            _error = result.Error.Description;
            return;
        }

        _success = "Caja abierta correctamente.";
        _openingAmount = 0;
        await RefreshSessionAsync();
    }

    private async Task CloseSessionAsync()
    {
        if (_currentSession is null)
            return;

        var result = await CloseSessionHandler.HandleAsync(new CloseCashSessionCommand(
            _currentSession.Id,
            _countedAmount,
            _closeNotes), await GetCurrentUserAsync());

        if (result.IsFailure)
        {
            _error = result.Error.Description;
            return;
        }

        _success = $"Caja cerrada. Diferencia: {Money(result.Value.Difference.GetValueOrDefault())}";
        _showCloseModal = false;
        _countedAmount = 0;
        _closeNotes = null;
        await RefreshSessionAsync();
    }

    private async Task ConfirmSaleAsync()
    {
        if (_context is null || _currentSession is null)
            return;

        var lines = _cart.Select(c => new CreateSaleLine(c.ProductId, c.Quantity, c.UnitPrice)).ToList();
        var payments = BuildSalePayments();
        if (payments is null)
            return;

        var tax = CheckoutCalculator.CalculateTax(
            Subtotal,
            _discount,
            _businessSettings?.UsesTaxes ?? false,
            _businessSettings?.DefaultTaxRate ?? 0m);

        var result = await CreateSaleHandler.HandleAsync(new CreateSaleCommand(
            _context.BusinessId,
            _context.BranchId,
            _context.CashRegisterId,
            SaleType.Counter,
            _discount,
            tax,
            lines,
            payments,
            Guid.TryParse(_selectedCustomerId, out var customerId) ? customerId : null), await GetCurrentUserAsync());

        if (result.IsFailure)
        {
            _error = result.Error.Description;
            return;
        }

        _success = $"Venta {result.Value.Number} confirmada. Total {Money(result.Value.Total)}";
        _lastSaleId = result.Value.SaleId;
        await PrintLastSaleAsync();

        _cart.Clear();
        _discount = 0;
        _tax = 0;
        foreach (var payment in _payments)
        {
            payment.Amount = 0;
            payment.Reference = null;
        }

        await RefreshSessionAsync();
    }

    private List<CreateSalePayment>? BuildSalePayments()
    {
        var result = CheckoutCalculator.BuildSalePayments(
            Total,
            BuildPaymentInputs());

        if (result.IsSuccess)
            return result.Payments;

        _error = result.Error is null ? null : $"{result.Error}";
        return null;
    }

    private void RecalculateTax()
    {
        _tax = CheckoutCalculator.CalculateTax(
            Subtotal,
            _discount,
            _businessSettings?.UsesTaxes ?? false,
            _businessSettings?.DefaultTaxRate ?? 0m);
    }

    private List<PosCartLineInput> BuildCartInputs() =>
        _cart
            .Select(line => new PosCartLineInput(line.Quantity, line.UnitPrice))
            .ToList();

    private List<PosPaymentInput> BuildPaymentInputs() =>
        _payments
            .Select(payment => new PosPaymentInput(
                payment.PaymentMethodId,
                payment.Code,
                payment.Name,
                payment.Type,
                payment.Amount,
                payment.Reference))
            .ToList();

    private decimal LineTotal(CartLine line) =>
        CheckoutCalculator.CalculateLineTotal(line.Quantity, line.UnitPrice);

    private string Money(decimal value) => $"{CurrencySymbol} {value:N2}";

    private static string FormatQuantity(CartLine line) =>
        line.AllowsFractionalQuantity ? line.Quantity.ToString("N2") : line.Quantity.ToString("N0");

    private static string Initials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return "PR";

        return string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0])));
    }

    private static string PaymentIcon(PaymentLine payment)
    {
        var value = $"{payment.Code} {payment.Name}".ToLowerInvariant();
        if (value.Contains("cash") || value.Contains("efectivo"))
            return "fa-money-bill-wave";
        if (value.Contains("card") || value.Contains("tarjeta"))
            return "fa-credit-card";
        if (value.Contains("qr") || value.Contains("transfer"))
            return "fa-qrcode";
        if (payment.Type == PaymentMethodType.Credit)
            return "fa-handshake";

        return "fa-wallet";
    }

    private static bool RequiresReference(PaymentLine payment) =>
        payment.Type is not PaymentMethodType.Cash and not PaymentMethodType.Credit;

    private async Task CreateCustomerAsync()
    {
        if (_context is null || string.IsNullOrWhiteSpace(_newCustomerName))
            return;

        var result = await CreateCustomerHandler.HandleAsync(new CreateCustomerCommand(
            _context.BusinessId,
            _newCustomerName));
        if (result.IsFailure)
        {
            _error = result.Error.Description;
            return;
        }

        await LoadCustomersAsync();
        _selectedCustomerId = result.Value.Id.ToString();
        _newCustomerName = string.Empty;
    }

    private async Task PrintLastSaleAsync()
    {
        if (_lastSaleId is null)
            return;

        await JS.InvokeVoidAsync("printReceipt", $"/api/receipts/sale/{_lastSaleId.Value}/thermal");
    }

    private async Task<string?> GetCurrentUserAsync()
    {
        return await CurrentUser.GetUserNameAsync();
    }

    private sealed class CartLine
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public bool AllowsFractionalQuantity { get; set; }
    }

    private sealed class PaymentLine(Guid paymentMethodId, string code, string name, PaymentMethodType type)
    {
        public Guid PaymentMethodId { get; } = paymentMethodId;
        public string Code { get; } = code;
        public string Name { get; } = name;
        public PaymentMethodType Type { get; } = type;
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
    }
}
