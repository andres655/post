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
using SmallBusinessPOS.Application.Features.PaymentMethods.GetActivePaymentMethods;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.Sales.CancelSale;
using SmallBusinessPOS.Application.Features.Sales.CreateSale;
using SmallBusinessPOS.Application.Features.Sales.GetDailyReport;

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

        return services;
    }
}
