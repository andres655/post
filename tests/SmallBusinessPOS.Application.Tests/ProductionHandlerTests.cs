using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Features.Production.ConfirmProductionEntry;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Tests;

public class ProductionHandlerTests
{
    private static IAppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;

        return new TestDbContext(options);
    }

    private static async Task<FixtureData> SeedFixtureAsync(IAppDbContext db, bool createStock = true)
    {
        var business = Business.Create("Pollo Sabroso", "DOP", "America/Santo_Domingo", BusinessType.RotisserieChicken);
        var branch = Branch.Create(business.Id, "Sucursal Principal", isMain: true);
        var product = Product.Create(
            business.Id,
            "POL-ENT",
            "Pollo horneado entero",
            ProductType.PreparedItem,
            UnitOfMeasure.Unit,
            650m,
            estimatedCost: 280m,
            tracksInventory: true);

        db.Businesses.Add(business);
        db.Branches.Add(branch);
        db.Products.Add(product);

        if (createStock)
            db.InventoryStocks.Add(InventoryStock.Create(business.Id, branch.Id, product.Id, 5m));

        await db.SaveChangesAsync();

        return new FixtureData(business.Id, branch.Id, product.Id);
    }

    [Fact]
    public async Task ConfirmProductionEntry_ShouldIncreaseInventory_AndCreateProductionOutputMovement()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);
        var handler = new ConfirmProductionEntryHandler(db, new ConfirmProductionEntryValidator());

        var result = await handler.HandleAsync(new ConfirmProductionEntryCommand(
            null,
            fixture.BusinessId,
            fixture.BranchId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            [new ConfirmProductionEntryLine(fixture.ProductId, 40m, 280m)],
            "Pollos preparados"), "supervisor@pollosaboroso.local");

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalQuantityProduced.Should().Be(40m);

        var stock = await db.InventoryStocks.SingleAsync(s => s.ProductId == fixture.ProductId);
        stock.Quantity.Should().Be(45m);

        var movement = await db.InventoryMovements.SingleAsync(m => m.MovementType == MovementType.ProductionOutput);
        movement.Quantity.Should().Be(40m);
        movement.PreviousQuantity.Should().Be(5m);
        movement.NewQuantity.Should().Be(45m);
        movement.ReferenceType.Should().Be("ProductionEntry");
        movement.ReferenceId.Should().Be(result.Value.ProductionEntryId);
    }

    [Fact]
    public async Task ConfirmProductionEntry_ShouldCreateStock_WhenProductHasNoStock()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db, createStock: false);
        var handler = new ConfirmProductionEntryHandler(db, new ConfirmProductionEntryValidator());

        var result = await handler.HandleAsync(new ConfirmProductionEntryCommand(
            null,
            fixture.BusinessId,
            fixture.BranchId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            [new ConfirmProductionEntryLine(fixture.ProductId, 12m, 280m)]));

        result.IsSuccess.Should().BeTrue();

        var stock = await db.InventoryStocks.SingleAsync(s => s.ProductId == fixture.ProductId);
        stock.Quantity.Should().Be(12m);
    }

    [Fact]
    public async Task ConfirmProductionEntry_ShouldRegisterWasteMovement_AndAddOnlyNetQuantity()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db, createStock: false);
        var handler = new ConfirmProductionEntryHandler(db, new ConfirmProductionEntryValidator());

        var result = await handler.HandleAsync(new ConfirmProductionEntryCommand(
            null,
            fixture.BusinessId,
            fixture.BranchId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            [new ConfirmProductionEntryLine(fixture.ProductId, 40m, 280m, QuantityWasted: 1m)],
            "Pollos preparados hoy: 40. Merma: 1."));

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalQuantityProduced.Should().Be(40m);
        result.Value.TotalQuantityWasted.Should().Be(1m);
        result.Value.NetQuantityAdded.Should().Be(39m);

        var stock = await db.InventoryStocks.SingleAsync(s => s.ProductId == fixture.ProductId);
        stock.Quantity.Should().Be(39m);

        var output = await db.InventoryMovements.SingleAsync(m => m.MovementType == MovementType.ProductionOutput);
        output.Quantity.Should().Be(40m);
        output.PreviousQuantity.Should().Be(0m);
        output.NewQuantity.Should().Be(40m);

        var waste = await db.InventoryMovements.SingleAsync(m => m.MovementType == MovementType.Waste);
        waste.Quantity.Should().Be(1m);
        waste.PreviousQuantity.Should().Be(40m);
        waste.NewQuantity.Should().Be(39m);
        waste.ReferenceType.Should().Be("ProductionEntry");
        waste.ReferenceId.Should().Be(result.Value.ProductionEntryId);
    }

    [Fact]
    public async Task ConfirmProductionEntry_ShouldFail_WhenAlreadyConfirmed()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);
        var entry = ProductionEntry.Create(
            fixture.BusinessId,
            fixture.BranchId,
            "PROD-20260716-0001",
            DateOnly.FromDateTime(DateTime.UtcNow));
        entry.AddDetail(ProductionEntryDetail.Create(entry.Id, fixture.ProductId, 2m));

        db.ProductionEntries.Add(entry);
        await db.SaveChangesAsync();

        var handler = new ConfirmProductionEntryHandler(db, new ConfirmProductionEntryValidator());
        var first = await handler.HandleAsync(new ConfirmProductionEntryCommand(
            entry.Id,
            fixture.BusinessId,
            fixture.BranchId,
            entry.ProductionDate,
            []));
        var second = await handler.HandleAsync(new ConfirmProductionEntryCommand(
            entry.Id,
            fixture.BusinessId,
            fixture.BranchId,
            entry.ProductionDate,
            []));

        first.IsSuccess.Should().BeTrue();
        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be("ProductionEntry.AlreadyConfirmed");

        var movementCount = await db.InventoryMovements.CountAsync(m => m.ReferenceId == entry.Id);
        movementCount.Should().Be(1);
    }

    [Fact]
    public async Task ConfirmProductionEntry_ShouldFail_WhenProductDoesNotTrackInventory()
    {
        var db = CreateDb();
        var business = Business.Create("Pollo Sabroso", "DOP", "America/Santo_Domingo", BusinessType.RotisserieChicken);
        var branch = Branch.Create(business.Id, "Sucursal Principal", isMain: true);
        var product = Product.Create(
            business.Id,
            "SERV",
            "Servicio",
            ProductType.Service,
            UnitOfMeasure.Unit,
            100m,
            tracksInventory: false);

        db.Businesses.Add(business);
        db.Branches.Add(branch);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new ConfirmProductionEntryHandler(db, new ConfirmProductionEntryValidator());
        var result = await handler.HandleAsync(new ConfirmProductionEntryCommand(
            null,
            business.Id,
            branch.Id,
            DateOnly.FromDateTime(DateTime.UtcNow),
            [new ConfirmProductionEntryLine(product.Id, 1m)]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProductionEntry.ProductDoesNotTrackInventory");
    }

    private sealed record FixtureData(Guid BusinessId, Guid BranchId, Guid ProductId);
}
