using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmallBusinessPOS.Application.Features.Categories.CreateCategory;
using SmallBusinessPOS.Application.Features.Categories.DisableCategory;
using SmallBusinessPOS.Application.Features.Categories.GetCategories;
using SmallBusinessPOS.Application.Features.Categories.GetCategory;
using SmallBusinessPOS.Application.Features.Categories.UpdateCategory;
using SmallBusinessPOS.Application.Features.Products.CreateProduct;
using SmallBusinessPOS.Application.Features.Products.DisableProduct;
using SmallBusinessPOS.Application.Features.Products.GetProduct;
using SmallBusinessPOS.Application.Features.Products.GetProducts;
using SmallBusinessPOS.Application.Features.Products.UpdateProduct;
using SmallBusinessPOS.Application.Features.CashSessions.CloseCashSession;
using SmallBusinessPOS.Application.Features.CashSessions.GetCurrentCashSession;
using SmallBusinessPOS.Application.Features.CashSessions.OpenCashSession;
using SmallBusinessPOS.Application.Features.Expenses.GetExpenses;
using SmallBusinessPOS.Application.Features.Expenses.RegisterExpense;
using SmallBusinessPOS.Application.Features.PaymentMethods.GetActivePaymentMethods;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.Production.ConfirmProductionEntry;
using SmallBusinessPOS.Application.Features.Production.GetProductionProducts;
using SmallBusinessPOS.Application.Features.Sales.CancelSale;
using SmallBusinessPOS.Application.Features.Sales.CreateSale;
using SmallBusinessPOS.Application.Features.Sales.GetCancellationHistory;
using SmallBusinessPOS.Application.Features.Sales.GetDailyReport;
using SmallBusinessPOS.Application.Features.Sales.GetSaleByNumber;

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
        services.AddScoped<CreateSaleValidator>();
        services.AddScoped<CancelSaleValidator>();
        services.AddScoped<RegisterExpenseValidator>();
        services.AddScoped<ConfirmProductionEntryValidator>();

        // Category handlers
        services.AddScoped<CreateCategoryHandler>();
        services.AddScoped<UpdateCategoryHandler>();
        services.AddScoped<GetCategoriesHandler>();
        services.AddScoped<GetCategoryHandler>();
        services.AddScoped<DisableCategoryHandler>();

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

        // POS and sales handlers
        services.AddScoped<GetPosContextHandler>();
        services.AddScoped<GetActivePaymentMethodsHandler>();
        services.AddScoped<CreateSaleHandler>();
        services.AddScoped<CancelSaleHandler>();
        services.AddScoped<GetDailyReportHandler>();
        services.AddScoped<GetSaleByNumberHandler>();
        services.AddScoped<GetCancellationHistoryHandler>();

        // Expense handlers
        services.AddScoped<RegisterExpenseHandler>();
        services.AddScoped<GetExpensesHandler>();

        // Production handlers
        services.AddScoped<ConfirmProductionEntryHandler>();
        services.AddScoped<GetProductionProductsHandler>();

        return services;
    }
}
