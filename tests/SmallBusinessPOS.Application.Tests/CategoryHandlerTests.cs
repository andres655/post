using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Features.Categories.CreateCategory;
using SmallBusinessPOS.Application.Features.Categories.DisableCategory;
using SmallBusinessPOS.Application.Features.Categories.GetCategories;
using SmallBusinessPOS.Application.Features.Categories.UpdateCategory;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Tests;

/// <summary>
/// Pruebas de los handlers de categorías usando base de datos InMemory.
/// </summary>
public class CategoryHandlerTests
{
    private static IAppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;
        return new TestDbContext(options);
    }

    private static async Task<(IAppDbContext db, Guid businessId)> CreateDbWithBusiness()
    {
        var db = CreateDb();
        var business = Business.Create("Negocio Test", "DOP", "America/Santo_Domingo", BusinessType.RotisserieChicken);
        db.Businesses.Add(business);
        await db.SaveChangesAsync();
        return (db, business.Id);
    }

    // ─── CreateCategory ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCategory_ValidCommand_ReturnsSuccess()
    {
        var (db, businessId) = await CreateDbWithBusiness();
        var handler = new CreateCategoryHandler(db, new CreateCategoryValidator());

        var result = await handler.HandleAsync(
            new CreateCategoryCommand(businessId, "Pollos", "Descripción", 1));

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Pollos");
        result.Value.BusinessId.Should().Be(businessId);
    }

    [Fact]
    public async Task CreateCategory_EmptyName_ReturnsValidationFailure()
    {
        var (db, businessId) = await CreateDbWithBusiness();
        var handler = new CreateCategoryHandler(db, new CreateCategoryValidator());

        var result = await handler.HandleAsync(
            new CreateCategoryCommand(businessId, "", null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Validation");
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_ReturnsConflictError()
    {
        var (db, businessId) = await CreateDbWithBusiness();
        var handler = new CreateCategoryHandler(db, new CreateCategoryValidator());

        await handler.HandleAsync(new CreateCategoryCommand(businessId, "Pollos", null));
        var result = await handler.HandleAsync(new CreateCategoryCommand(businessId, "Pollos", null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.DuplicateName");
    }

    [Fact]
    public async Task CreateCategory_BusinessNotFound_ReturnsNotFoundError()
    {
        var db = CreateDb();
        var handler = new CreateCategoryHandler(db, new CreateCategoryValidator());

        var result = await handler.HandleAsync(
            new CreateCategoryCommand(Guid.CreateVersion7(), "Pollos", null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ─── UpdateCategory ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCategory_ValidCommand_ReturnsSuccess()
    {
        var (db, businessId) = await CreateDbWithBusiness();
        var createHandler = new CreateCategoryHandler(db, new CreateCategoryValidator());
        var created = await createHandler.HandleAsync(new CreateCategoryCommand(businessId, "Pollos", null));

        var updateHandler = new UpdateCategoryHandler(db, new UpdateCategoryValidator());
        var result = await updateHandler.HandleAsync(
            new UpdateCategoryCommand(created.Value.Id, "Pollos Horneados", "Nueva descripción", 2));

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Pollos Horneados");
    }

    [Fact]
    public async Task UpdateCategory_NotFound_ReturnsError()
    {
        var db = CreateDb();
        var handler = new UpdateCategoryHandler(db, new UpdateCategoryValidator());

        var result = await handler.HandleAsync(
            new UpdateCategoryCommand(Guid.CreateVersion7(), "Test", null, 0));

        result.IsFailure.Should().BeTrue();
    }

    // ─── DisableCategory ──────────────────────────────────────────────────────

    [Fact]
    public async Task DisableCategory_ActiveCategory_Succeeds()
    {
        var (db, businessId) = await CreateDbWithBusiness();
        var createHandler = new CreateCategoryHandler(db, new CreateCategoryValidator());
        var created = await createHandler.HandleAsync(new CreateCategoryCommand(businessId, "Pollos", null));

        var disableHandler = new DisableCategoryHandler(db);
        var result = await disableHandler.HandleAsync(new DisableCategoryCommand(created.Value.Id));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DisableCategory_AlreadyDisabled_ReturnsError()
    {
        var (db, businessId) = await CreateDbWithBusiness();
        var createHandler = new CreateCategoryHandler(db, new CreateCategoryValidator());
        var created = await createHandler.HandleAsync(new CreateCategoryCommand(businessId, "Pollos", null));

        var disableHandler = new DisableCategoryHandler(db);
        await disableHandler.HandleAsync(new DisableCategoryCommand(created.Value.Id));
        var result = await disableHandler.HandleAsync(new DisableCategoryCommand(created.Value.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.AlreadyDisabled");
    }

    // ─── GetCategories ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCategories_ReturnsOnlyActiveByDefault()
    {
        var (db, businessId) = await CreateDbWithBusiness();
        var createHandler = new CreateCategoryHandler(db, new CreateCategoryValidator());
        var created1 = await createHandler.HandleAsync(new CreateCategoryCommand(businessId, "Pollos", null));
        var created2 = await createHandler.HandleAsync(new CreateCategoryCommand(businessId, "Bebidas", null));

        var disableHandler = new DisableCategoryHandler(db);
        await disableHandler.HandleAsync(new DisableCategoryCommand(created2.Value.Id));

        var getHandler = new GetCategoriesHandler(db);
        var result = await getHandler.HandleAsync(new GetCategoriesQuery(businessId, OnlyActive: true));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Name.Should().Be("Pollos");
    }

    [Fact]
    public async Task GetCategories_IncludesInactive_WhenRequested()
    {
        var (db, businessId) = await CreateDbWithBusiness();
        var createHandler = new CreateCategoryHandler(db, new CreateCategoryValidator());
        var created1 = await createHandler.HandleAsync(new CreateCategoryCommand(businessId, "Pollos", null));
        var created2 = await createHandler.HandleAsync(new CreateCategoryCommand(businessId, "Bebidas", null));

        var disableHandler = new DisableCategoryHandler(db);
        await disableHandler.HandleAsync(new DisableCategoryCommand(created2.Value.Id));

        var getHandler = new GetCategoriesHandler(db);
        var result = await getHandler.HandleAsync(new GetCategoriesQuery(businessId, OnlyActive: false));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}
