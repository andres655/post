using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Features.Products.CreateProduct;
using SmallBusinessPOS.Application.Features.Products.DisableProduct;
using SmallBusinessPOS.Application.Features.Products.GetProduct;
using SmallBusinessPOS.Application.Features.Products.GetProducts;
using SmallBusinessPOS.Application.Features.Products.UpdateProduct;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Tests;

public class ProductHandlerTests
{
    private static IAppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        return new TestDbContext(options);
    }

    private static async Task<(IAppDbContext db, Guid businessId, Guid categoryId)> CreateDbWithBusinessAndCategory()
    {
        var db = CreateDb();
        var business = Business.Create("Negocio Test", "DOP", "America/Santo_Domingo", BusinessType.RotisserieChicken);
        db.Businesses.Add(business);

        var category = Category.Create(business.Id, "Pollos");
        db.Categories.Add(category);

        await db.SaveChangesAsync();
        return (db, business.Id, category.Id);
    }

    // ─── CreateProduct ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateProduct_ValidCommand_ReturnsSuccess()
    {
        var (db, businessId, categoryId) = await CreateDbWithBusinessAndCategory();
        var handler = new CreateProductHandler(db, new CreateProductValidator());

        var result = await handler.HandleAsync(new CreateProductCommand(
            businessId, "POL-ENT", "Pollo entero",
            ProductType.PreparedItem, UnitOfMeasure.Unit,
            SalePrice: 650m, EstimatedCost: 280m, CategoryId: categoryId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("POL-ENT");
        result.Value.SalePrice.Should().Be(650m);
    }

    [Fact]
    public async Task CreateProduct_NegativePrice_ReturnsValidationError()
    {
        var (db, businessId, _) = await CreateDbWithBusinessAndCategory();
        var handler = new CreateProductHandler(db, new CreateProductValidator());

        var result = await handler.HandleAsync(new CreateProductCommand(
            businessId, "P001", "Test",
            ProductType.Standard, UnitOfMeasure.Unit,
            SalePrice: -10m));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Validation");
    }

    [Fact]
    public async Task CreateProduct_DuplicateCode_ReturnsConflictError()
    {
        var (db, businessId, _) = await CreateDbWithBusinessAndCategory();
        var handler = new CreateProductHandler(db, new CreateProductValidator());

        await handler.HandleAsync(new CreateProductCommand(
            businessId, "POL-ENT", "Pollo entero",
            ProductType.Standard, UnitOfMeasure.Unit, 100m));

        var result = await handler.HandleAsync(new CreateProductCommand(
            businessId, "pol-ent", "Otro pollo",
            ProductType.Standard, UnitOfMeasure.Unit, 100m));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.DuplicateCode");
    }

    [Fact]
    public async Task CreateProduct_EmptyCode_ReturnsValidationError()
    {
        var (db, businessId, _) = await CreateDbWithBusinessAndCategory();
        var handler = new CreateProductHandler(db, new CreateProductValidator());

        var result = await handler.HandleAsync(new CreateProductCommand(
            businessId, "", "Test",
            ProductType.Standard, UnitOfMeasure.Unit, 0m));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task CreateProduct_CategoryBelongsToDifferentBusiness_ReturnsNotFound()
    {
        var (db, businessId, _) = await CreateDbWithBusinessAndCategory();
        var handler = new CreateProductHandler(db, new CreateProductValidator());

        var result = await handler.HandleAsync(new CreateProductCommand(
            businessId, "P001", "Test",
            ProductType.Standard, UnitOfMeasure.Unit, 100m,
            CategoryId: Guid.CreateVersion7())); // Id que no existe

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ─── DisableProduct ───────────────────────────────────────────────────────

    [Fact]
    public async Task DisableProduct_ActiveProduct_Succeeds()
    {
        var (db, businessId, _) = await CreateDbWithBusinessAndCategory();
        var createHandler = new CreateProductHandler(db, new CreateProductValidator());
        var created = await createHandler.HandleAsync(new CreateProductCommand(
            businessId, "P001", "Test", ProductType.Standard, UnitOfMeasure.Unit, 100m));

        var disableHandler = new DisableProductHandler(db);
        var result = await disableHandler.HandleAsync(new DisableProductCommand(created.Value.Id));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DisableProduct_AlreadyDisabled_ReturnsError()
    {
        var (db, businessId, _) = await CreateDbWithBusinessAndCategory();
        var createHandler = new CreateProductHandler(db, new CreateProductValidator());
        var created = await createHandler.HandleAsync(new CreateProductCommand(
            businessId, "P001", "Test", ProductType.Standard, UnitOfMeasure.Unit, 100m));

        var disableHandler = new DisableProductHandler(db);
        await disableHandler.HandleAsync(new DisableProductCommand(created.Value.Id));
        var result = await disableHandler.HandleAsync(new DisableProductCommand(created.Value.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.AlreadyDisabled");
    }

    // ─── GetProducts ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProducts_FilterByCategory_ReturnsOnlyMatchingProducts()
    {
        var (db, businessId, categoryId) = await CreateDbWithBusinessAndCategory();
        var createHandler = new CreateProductHandler(db, new CreateProductValidator());

        await createHandler.HandleAsync(new CreateProductCommand(
            businessId, "P001", "Pollo entero",
            ProductType.PreparedItem, UnitOfMeasure.Unit, 650m, CategoryId: categoryId));

        await createHandler.HandleAsync(new CreateProductCommand(
            businessId, "P002", "Refresco",
            ProductType.Standard, UnitOfMeasure.Unit, 60m));

        var getHandler = new GetProductsHandler(db);
        var result = await getHandler.HandleAsync(
            new GetProductsQuery(businessId, CategoryId: categoryId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Code.Should().Be("P001");
    }

    [Fact]
    public async Task GetProducts_SearchByName_ReturnsMatchingProducts()
    {
        var (db, businessId, _) = await CreateDbWithBusinessAndCategory();
        var createHandler = new CreateProductHandler(db, new CreateProductValidator());

        await createHandler.HandleAsync(new CreateProductCommand(
            businessId, "P001", "Pollo horneado entero",
            ProductType.PreparedItem, UnitOfMeasure.Unit, 650m));

        await createHandler.HandleAsync(new CreateProductCommand(
            businessId, "P002", "Refresco de cola",
            ProductType.Standard, UnitOfMeasure.Unit, 60m));

        var getHandler = new GetProductsHandler(db);
        var result = await getHandler.HandleAsync(
            new GetProductsQuery(businessId, SearchTerm: "pollo"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    // ─── GetProduct ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProduct_ExistingId_ReturnsProduct()
    {
        var (db, businessId, categoryId) = await CreateDbWithBusinessAndCategory();
        var createHandler = new CreateProductHandler(db, new CreateProductValidator());
        var created = await createHandler.HandleAsync(new CreateProductCommand(
            businessId, "P001", "Pollo entero",
            ProductType.PreparedItem, UnitOfMeasure.Unit, 650m, CategoryId: categoryId));

        var getHandler = new GetProductHandler(db);
        var result = await getHandler.HandleAsync(new GetProductQuery(created.Value.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(created.Value.Id);
    }

    [Fact]
    public async Task GetProduct_NotFound_ReturnsError()
    {
        var db = CreateDb();
        var handler = new GetProductHandler(db);
        var result = await handler.HandleAsync(new GetProductQuery(Guid.CreateVersion7()));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ─── UpdateProduct ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProduct_ChangesPrice()
    {
        var (db, businessId, _) = await CreateDbWithBusinessAndCategory();
        var createHandler = new CreateProductHandler(db, new CreateProductValidator());
        var created = await createHandler.HandleAsync(new CreateProductCommand(
            businessId, "P001", "Test",
            ProductType.Standard, UnitOfMeasure.Unit, 100m));

        var updateHandler = new UpdateProductHandler(db, new UpdateProductValidator());
        var result = await updateHandler.HandleAsync(new UpdateProductCommand(
            created.Value.Id, "P001", "Test Actualizado",
            ProductType.Standard, UnitOfMeasure.Unit,
            SalePrice: 150m, EstimatedCost: 0m,
            CategoryId: null, TracksInventory: true,
            AllowsFractionalQuantity: false));

        result.IsSuccess.Should().BeTrue();
        result.Value.SalePrice.Should().Be(150m);
        result.Value.Name.Should().Be("Test Actualizado");
    }
}
