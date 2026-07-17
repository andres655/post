using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Features.Inventory.AdjustInventory;
using SmallBusinessPOS.Application.Features.Inventory.GetInventoryMovements;
using SmallBusinessPOS.Application.Features.Inventory.GetInventoryOverview;
using SmallBusinessPOS.Application.Features.Inventory.SetMinimumStock;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Tests;

public class InventoryHandlerTests
{
    private static IAppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task GetInventoryOverview_ShouldShowCurrentStockAndLowStockAlerts()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);
        var stock = await db.InventoryStocks.SingleAsync();
        stock.SetMinimumQuantity(10m);
        await db.SaveChangesAsync();

        var handler = new GetInventoryOverviewHandler(db);
        var result = await handler.HandleAsync(new GetInventoryOverviewQuery(fixture.BusinessId, fixture.BranchId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Quantity.Should().Be(5m);
        result.Value[0].MinimumQuantity.Should().Be(10m);
        result.Value[0].IsLowStock.Should().BeTrue();
    }

    [Fact]
    public async Task AdjustInventory_ShouldUpdateStockAndCreateMovement()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);

        var handler = new AdjustInventoryHandler(db, new AdjustInventoryValidator());
        var result = await handler.HandleAsync(new AdjustInventoryCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.ProductId,
            3m,
            "Conteo físico"));

        result.IsSuccess.Should().BeTrue();
        result.Value.PreviousQuantity.Should().Be(5m);
        result.Value.NewQuantity.Should().Be(8m);

        var movement = await db.InventoryMovements.SingleAsync();
        movement.MovementType.Should().Be(MovementType.AdjustmentIncrease);
        movement.Quantity.Should().Be(3m);

        var movements = await new GetInventoryMovementsHandler(db)
            .HandleAsync(new GetInventoryMovementsQuery(fixture.BusinessId, fixture.BranchId, fixture.ProductId));
        movements.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task AdjustInventory_ShouldFail_WhenNegativeStockIsNotAllowed()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);

        var handler = new AdjustInventoryHandler(db, new AdjustInventoryValidator());
        var result = await handler.HandleAsync(new AdjustInventoryCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.ProductId,
            -6m,
            "Conteo físico"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Inventory.NegativeStockNotAllowed");
    }

    [Fact]
    public async Task SetMinimumStock_ShouldUpdateMinimumQuantity()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);

        var handler = new SetMinimumStockHandler(db, new SetMinimumStockValidator());
        var result = await handler.HandleAsync(new SetMinimumStockCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.ProductId,
            12m));

        result.IsSuccess.Should().BeTrue();

        var stock = await db.InventoryStocks.SingleAsync();
        stock.MinimumQuantity.Should().Be(12m);
        stock.IsBelowMinimum().Should().BeTrue();
    }

    private static async Task<FixtureData> SeedFixtureAsync(IAppDbContext db)
    {
        var business = Business.Create("Pollo Sabroso", "DOP", "America/Santo_Domingo", BusinessType.RotisserieChicken);
        var branch = Branch.Create(business.Id, "Sucursal Principal", isMain: true);
        var product = Product.Create(
            business.Id,
            "POL-ENT",
            "Pollo entero",
            ProductType.PreparedItem,
            UnitOfMeasure.Unit,
            650m,
            tracksInventory: true);

        db.Businesses.Add(business);
        db.Branches.Add(branch);
        db.BusinessSettings.Add(BusinessSettings.CreateDefault(business.Id));
        db.Products.Add(product);
        db.InventoryStocks.Add(InventoryStock.Create(business.Id, branch.Id, product.Id, 5m));
        await db.SaveChangesAsync();

        return new FixtureData(business.Id, branch.Id, product.Id);
    }

    private sealed record FixtureData(Guid BusinessId, Guid BranchId, Guid ProductId);
}
