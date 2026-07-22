using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmallBusinessPOS.Application.Features.Categories.CreateCategory;
using SmallBusinessPOS.Application.Features.Categories.DisableCategory;
using SmallBusinessPOS.Application.Features.Categories.GetCategories;
using SmallBusinessPOS.Application.Features.Categories.GetCategory;
using SmallBusinessPOS.Application.Features.Categories.UpdateCategory;
using SmallBusinessPOS.Application.Features.Customers.CreateCustomer;
using SmallBusinessPOS.Application.Features.Customers.GetCustomers;
using SmallBusinessPOS.Application.Features.Products.CreateProduct;
using SmallBusinessPOS.Application.Features.Products.DisableProduct;
using SmallBusinessPOS.Application.Features.Products.GetProduct;
using SmallBusinessPOS.Application.Features.Products.GetProducts;
using SmallBusinessPOS.Application.Features.Products.UpdateProduct;
using SmallBusinessPOS.Application.Features.CashSessions.CloseCashSession;
using SmallBusinessPOS.Application.Features.CashSessions.GetCashSessionHistory;
using SmallBusinessPOS.Application.Features.CashSessions.GetCashSessionSummary;
using SmallBusinessPOS.Application.Features.CashSessions.GetCurrentCashSession;
using SmallBusinessPOS.Application.Features.CashSessions.OpenCashSession;
using SmallBusinessPOS.Application.Features.CashSessions.RegisterCashWithdrawal;
using SmallBusinessPOS.Application.Features.Expenses.GetExpenses;
using SmallBusinessPOS.Application.Features.Expenses.RegisterExpense;
using SmallBusinessPOS.Application.Features.Inventory.AdjustInventory;
using SmallBusinessPOS.Application.Features.Inventory.GetInventoryMovements;
using SmallBusinessPOS.Application.Features.Inventory.GetInventoryOverview;
using SmallBusinessPOS.Application.Features.Inventory.SetMinimumStock;
using SmallBusinessPOS.Application.Features.PaymentMethods.GetActivePaymentMethods;
using SmallBusinessPOS.Application.Features.POS.Checkout;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.POS.GetPosOptions;
using SmallBusinessPOS.Application.Features.Production.CancelProductionEntry;
using SmallBusinessPOS.Application.Features.Production.ConfirmProductionEntry;
using SmallBusinessPOS.Application.Features.Production.GetProductionHistory;
using SmallBusinessPOS.Application.Features.Production.GetProductionInputProducts;
using SmallBusinessPOS.Application.Features.Production.GetProductionProducts;
using SmallBusinessPOS.Application.Features.Production.GetProductionRecipe;
using SmallBusinessPOS.Application.Features.Production.SaveProductionRecipe;
using SmallBusinessPOS.Application.Features.Receipts.GetSaleReceipt;
using SmallBusinessPOS.Application.Features.Receipts.GetSaleReceiptLookup;
using SmallBusinessPOS.Application.Features.Receipts.GenerateSaleReceiptPdf;
using SmallBusinessPOS.Application.Features.Receipts.GenerateSaleReceiptPdfByNumber;
using SmallBusinessPOS.Application.Features.Receipts.RegisterReceiptReprint;
using SmallBusinessPOS.Application.Features.Receipts.ReprintSaleReceiptPdf;
using SmallBusinessPOS.Application.Features.Receipts.ReprintSaleReceiptPdfByNumber;
using SmallBusinessPOS.Application.Features.Receipts.ReprintSaleReceiptThermal;
using SmallBusinessPOS.Application.Features.Receipts.ReprintSaleReceiptThermalByNumber;
using SmallBusinessPOS.Application.Features.Reports.GetManagementDashboard;
using SmallBusinessPOS.Application.Features.Reports.GetOperationalAudit;
using SmallBusinessPOS.Application.Features.Reports.GetProfitabilityReport;
using SmallBusinessPOS.Application.Features.Sales.CancelSale;
using SmallBusinessPOS.Application.Features.Sales.CreateSale;
using SmallBusinessPOS.Application.Features.Sales.GetCancellationHistory;
using SmallBusinessPOS.Application.Features.Sales.GetDailyReport;
using SmallBusinessPOS.Application.Features.Sales.GetSaleForReturn;
using SmallBusinessPOS.Application.Features.Sales.GetSaleByNumber;
using SmallBusinessPOS.Application.Features.Sales.RegisterSaleReturn;
using SmallBusinessPOS.Application.Features.Settings.GetBusinessSettings;
using SmallBusinessPOS.Application.Features.Settings.UpdateBusinessSettings;
using SmallBusinessPOS.Application.Features.Users.ChangeUserStatus;
using SmallBusinessPOS.Application.Features.Users.CreateUser;
using SmallBusinessPOS.Application.Features.Users.GetUsers;
using SmallBusinessPOS.Application.Features.Users.ResetUserPassword;
using SmallBusinessPOS.Application.Features.Users.UpdateUserRoles;

namespace SmallBusinessPOS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Validators
        services.AddScoped<CreateCategoryValidator>();
        services.AddScoped<UpdateCategoryValidator>();
        services.AddScoped<CreateProductValidator>();
        services.AddScoped<UpdateProductValidator>();
        services.AddScoped<OpenCashSessionValidator>();
        services.AddScoped<CloseCashSessionValidator>();
        services.AddScoped<RegisterCashWithdrawalValidator>();
        services.AddScoped<CreateSaleValidator>();
        services.AddScoped<CancelSaleValidator>();
        services.AddScoped<RegisterExpenseValidator>();
        services.AddScoped<Features.ExpenseCategories.CreateExpenseCategory.CreateExpenseCategoryValidator>();
        services.AddScoped<CancelProductionEntryValidator>();
        services.AddScoped<ConfirmProductionEntryValidator>();
        services.AddScoped<SaveProductionRecipeValidator>();
        services.AddScoped<AdjustInventoryValidator>();
        services.AddScoped<SetMinimumStockValidator>();
        services.AddScoped<UpdateBusinessSettingsValidator>();
        services.AddScoped<CreateUserValidator>();
        services.AddScoped<CreateCustomerValidator>();
        services.AddScoped<RegisterSaleReturnValidator>();

        // Category handlers
        services.AddScoped<CreateCategoryHandler>();
        services.AddScoped<UpdateCategoryHandler>();
        services.AddScoped<GetCategoriesHandler>();
        services.AddScoped<GetCategoryHandler>();
        services.AddScoped<DisableCategoryHandler>();
        services.AddScoped<GetCustomersHandler>();
        services.AddScoped<CreateCustomerHandler>();

        // Product handlers
        services.AddScoped<CreateProductHandler>();
        services.AddScoped<UpdateProductHandler>();
        services.AddScoped<GetProductsHandler>();
        services.AddScoped<GetProductHandler>();
        services.AddScoped<DisableProductHandler>();

        // Cash session handlers
        services.AddScoped<OpenCashSessionHandler>();
        services.AddScoped<CloseCashSessionHandler>();
        services.AddScoped<GetCurrentCashSessionHandler>();
        services.AddScoped<GetCashSessionSummaryHandler>();
        services.AddScoped<GetCashSessionHistoryHandler>();
        services.AddScoped<RegisterCashWithdrawalHandler>();

        // POS and sales handlers
        services.AddScoped<GetPosContextHandler>();
        services.AddScoped<GetPosOptionsHandler>();
        services.AddScoped<GetActivePaymentMethodsHandler>();
        services.AddScoped<CreateSaleHandler>();
        services.AddScoped<CancelSaleHandler>();
        services.AddScoped<GetDailyReportHandler>();
        services.AddScoped<GetManagementDashboardHandler>();
        services.AddScoped<GetOperationalAuditHandler>();
        services.AddScoped<GetProfitabilityReportHandler>();
        services.AddScoped<GetSaleByNumberHandler>();
        services.AddScoped<GetSaleForReturnHandler>();
        services.AddScoped<GetCancellationHistoryHandler>();
        services.AddScoped<RegisterSaleReturnHandler>();
        services.AddScoped<GetSaleReceiptHandler>();
        services.AddScoped<GetSaleReceiptLookupHandler>();
        services.AddScoped<GenerateSaleReceiptPdfHandler>();
        services.AddScoped<GenerateSaleReceiptPdfByNumberHandler>();
        services.AddScoped<RegisterReceiptReprintHandler>();
        services.AddScoped<ReprintSaleReceiptPdfHandler>();
        services.AddScoped<ReprintSaleReceiptPdfByNumberHandler>();
        services.AddScoped<ReprintSaleReceiptThermalHandler>();
        services.AddScoped<ReprintSaleReceiptThermalByNumberHandler>();
        services.AddScoped<PosCheckoutCalculator>();
        services.AddScoped<GetBusinessSettingsHandler>();
        services.AddScoped<UpdateBusinessSettingsHandler>();
        services.AddScoped<GetUsersHandler>();
        services.AddScoped<CreateUserHandler>();
        services.AddScoped<UpdateUserRolesHandler>();
        services.AddScoped<ResetUserPasswordHandler>();
        services.AddScoped<ChangeUserStatusHandler>();

        // Expense handlers
        services.AddScoped<RegisterExpenseHandler>();
        services.AddScoped<GetExpensesHandler>();
        services.AddScoped<Features.ExpenseCategories.GetExpenseCategories.GetExpenseCategoriesHandler>();
        services.AddScoped<Features.ExpenseCategories.CreateExpenseCategory.CreateExpenseCategoryHandler>();
        services.AddScoped<Features.ProductTypes.GetProductTypes.GetProductTypesHandler>();

        // Production handlers
        services.AddScoped<CancelProductionEntryHandler>();
        services.AddScoped<ConfirmProductionEntryHandler>();
        services.AddScoped<GetProductionHistoryHandler>();
        services.AddScoped<GetProductionInputProductsHandler>();
        services.AddScoped<GetProductionProductsHandler>();
        services.AddScoped<GetProductionRecipeHandler>();
        services.AddScoped<SaveProductionRecipeHandler>();

        // Inventory handlers
        services.AddScoped<GetInventoryOverviewHandler>();
        services.AddScoped<GetInventoryMovementsHandler>();
        services.AddScoped<AdjustInventoryHandler>();
        services.AddScoped<SetMinimumStockHandler>();

        return services;
    }
}
