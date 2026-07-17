using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Features.Production.CancelProductionEntry;
using SmallBusinessPOS.Application.Features.Production.ConfirmProductionEntry;
using SmallBusinessPOS.Application.Features.Production.GetProductionHistory;
using SmallBusinessPOS.Application.Features.Production.GetProductionRecipe;
using SmallBusinessPOS.Application.Features.Production.SaveProductionRecipe;
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
        var ribs = Product.Create(
            business.Id,
            "CST-ENT",
            "Costilla horneada",
            ProductType.PreparedItem,
            UnitOfMeasure.Unit,
            800m,
            estimatedCost: 350m,
            tracksInventory: true);
        var seasoning = Product.Create(
            business.Id,
            "SZN-BBQ",
            "Sazon BBQ",
            ProductType.Ingredient,
            UnitOfMeasure.Kilogram,
            0m,
            estimatedCost: 120m,
            tracksInventory: true,
            allowsFractionalQuantity: true);

        db.Businesses.Add(business);
        db.Branches.Add(branch);
        db.Products.Add(product);
        db.Products.Add(ribs);
        db.Products.Add(seasoning);

        if (createStock)
        {
            db.InventoryStocks.Add(InventoryStock.Create(business.Id, branch.Id, product.Id, 5m));
            db.InventoryStocks.Add(InventoryStock.Create(business.Id, branch.Id, ribs.Id, 2m));
            db.InventoryStocks.Add(InventoryStock.Create(business.Id, branch.Id, seasoning.Id, 20m));
        }

        await db.SaveChangesAsync();

        return new FixtureData(business.Id, branch.Id, product.Id, ribs.Id, seasoning.Id);
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
    public async Task ConfirmProductionEntry_ShouldSupportMultipleProducts()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);
        var handler = new ConfirmProductionEntryHandler(db, new ConfirmProductionEntryValidator());

        var result = await handler.HandleAsync(new ConfirmProductionEntryCommand(
            null,
            fixture.BusinessId,
            fixture.BranchId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            [
                new ConfirmProductionEntryLine(fixture.ProductId, 10m, 280m, 1m),
                new ConfirmProductionEntryLine(fixture.SecondProductId, 4m, 350m)
            ]));

        result.IsSuccess.Should().BeTrue();
        result.Value.DetailCount.Should().Be(2);
        result.Value.NetQuantityAdded.Should().Be(13m);

        var firstStock = await db.InventoryStocks.SingleAsync(s => s.ProductId == fixture.ProductId);
        var secondStock = await db.InventoryStocks.SingleAsync(s => s.ProductId == fixture.SecondProductId);
        firstStock.Quantity.Should().Be(14m);
        secondStock.Quantity.Should().Be(6m);
    }

    [Fact]
    public async Task ConfirmProductionEntry_ShouldConsumeInputsWithProductionInputMovement()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);
        var handler = new ConfirmProductionEntryHandler(db, new ConfirmProductionEntryValidator());

        var result = await handler.HandleAsync(new ConfirmProductionEntryCommand(
            null,
            fixture.BusinessId,
            fixture.BranchId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            [new ConfirmProductionEntryLine(fixture.ProductId, 10m, 280m)],
            Inputs: [new ConfirmProductionInputLine(fixture.InputProductId, 2.5m)]));

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalInputConsumed.Should().Be(2.5m);

        var inputStock = await db.InventoryStocks.SingleAsync(s => s.ProductId == fixture.InputProductId);
        inputStock.Quantity.Should().Be(17.5m);

        var inputMovement = await db.InventoryMovements.SingleAsync(m => m.MovementType == MovementType.ProductionInput);
        inputMovement.Quantity.Should().Be(2.5m);
        inputMovement.PreviousQuantity.Should().Be(20m);
        inputMovement.NewQuantity.Should().Be(17.5m);
        inputMovement.ReferenceId.Should().Be(result.Value.ProductionEntryId);
    }

    [Fact]
    public async Task SaveProductionRecipe_ShouldPersistRecipeComponents()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);

        var save = new SaveProductionRecipeHandler(db, new SaveProductionRecipeValidator());
        var result = await save.HandleAsync(new SaveProductionRecipeCommand(
            fixture.BusinessId,
            fixture.ProductId,
            [new SaveProductionRecipeComponent(fixture.InputProductId, 0.25m)]));

        result.IsSuccess.Should().BeTrue();

        var recipe = await new GetProductionRecipeHandler(db)
            .HandleAsync(new GetProductionRecipeQuery(fixture.BusinessId, fixture.ProductId));

        recipe.IsSuccess.Should().BeTrue();
        recipe.Value.Components.Should().ContainSingle();
        recipe.Value.Components[0].ProductId.Should().Be(fixture.InputProductId);
        recipe.Value.Components[0].Quantity.Should().Be(0.25m);
    }

    [Fact]
    public async Task ConfirmProductionEntry_ShouldCalculateInputsFromRecipe_WhenManualInputsAreEmpty()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);
        db.ProductComponents.Add(ProductComponent.Create(fixture.ProductId, fixture.InputProductId, 0.25m));
        await db.SaveChangesAsync();

        var handler = new ConfirmProductionEntryHandler(db, new ConfirmProductionEntryValidator());

        var result = await handler.HandleAsync(new ConfirmProductionEntryCommand(
            null,
            fixture.BusinessId,
            fixture.BranchId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            [new ConfirmProductionEntryLine(fixture.ProductId, 10m, 280m)]));

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalInputConsumed.Should().Be(2.5m);

        var inputStock = await db.InventoryStocks.SingleAsync(s => s.ProductId == fixture.InputProductId);
        inputStock.Quantity.Should().Be(17.5m);

        var inputMovement = await db.InventoryMovements.SingleAsync(m => m.MovementType == MovementType.ProductionInput);
        inputMovement.Quantity.Should().Be(2.5m);
    }

    [Fact]
    public async Task ConfirmProductionEntry_ShouldFail_WhenRecipeInputStockIsInsufficient()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);
        db.ProductComponents.Add(ProductComponent.Create(fixture.ProductId, fixture.InputProductId, 3m));
        await db.SaveChangesAsync();

        var handler = new ConfirmProductionEntryHandler(db, new ConfirmProductionEntryValidator());

        var result = await handler.HandleAsync(new ConfirmProductionEntryCommand(
            null,
            fixture.BusinessId,
            fixture.BranchId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            [new ConfirmProductionEntryLine(fixture.ProductId, 10m, 280m)]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProductionEntry.InsufficientInputStock");
    }

    [Fact]
    public async Task ConfirmProductionEntry_ShouldFail_WhenInputStockIsInsufficient()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);
        var handler = new ConfirmProductionEntryHandler(db, new ConfirmProductionEntryValidator());

        var result = await handler.HandleAsync(new ConfirmProductionEntryCommand(
            null,
            fixture.BusinessId,
            fixture.BranchId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            [new ConfirmProductionEntryLine(fixture.ProductId, 10m, 280m)],
            Inputs: [new ConfirmProductionInputLine(fixture.InputProductId, 25m)]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProductionEntry.InsufficientInputStock");
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
    public async Task GetProductionHistory_ShouldReturnEntriesWithDetails()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);
        var confirm = new ConfirmProductionEntryHandler(db, new ConfirmProductionEntryValidator());

        var confirmed = await confirm.HandleAsync(new ConfirmProductionEntryCommand(
            null,
            fixture.BusinessId,
            fixture.BranchId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            [
                new ConfirmProductionEntryLine(fixture.ProductId, 10m, 280m, 1m),
                new ConfirmProductionEntryLine(fixture.SecondProductId, 4m, 350m)
            ]));
        confirmed.IsSuccess.Should().BeTrue();

        var history = await new GetProductionHistoryHandler(db)
            .HandleAsync(new GetProductionHistoryQuery(fixture.BusinessId, fixture.BranchId));

        history.IsSuccess.Should().BeTrue();
        history.Value.Should().ContainSingle();
        history.Value[0].Details.Should().HaveCount(2);
        history.Value[0].TotalProduced.Should().Be(14m);
        history.Value[0].TotalWasted.Should().Be(1m);
        history.Value[0].NetAdded.Should().Be(13m);
    }

    [Fact]
    public async Task GetProductionHistory_ShouldReturnConsumedInputs()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);
        var confirm = new ConfirmProductionEntryHandler(db, new ConfirmProductionEntryValidator());

        var confirmed = await confirm.HandleAsync(new ConfirmProductionEntryCommand(
            null,
            fixture.BusinessId,
            fixture.BranchId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            [new ConfirmProductionEntryLine(fixture.ProductId, 10m, 280m)],
            Inputs: [new ConfirmProductionInputLine(fixture.InputProductId, 2.5m)]));
        confirmed.IsSuccess.Should().BeTrue();

        var history = await new GetProductionHistoryHandler(db)
            .HandleAsync(new GetProductionHistoryQuery(fixture.BusinessId, fixture.BranchId));

        history.Value.Should().ContainSingle();
        history.Value[0].Inputs.Should().ContainSingle();
        history.Value[0].Inputs[0].Quantity.Should().Be(2.5m);
        history.Value[0].DirectCost.Should().Be(2800m);
        history.Value[0].InputCost.Should().Be(300m);
        history.Value[0].TotalCost.Should().Be(3100m);
        history.Value[0].CostPerNetUnit.Should().Be(310m);
        history.Value[0].Inputs[0].EstimatedUnitCost.Should().Be(120m);
        history.Value[0].Inputs[0].TotalCost.Should().Be(300m);
    }

    [Fact]
    public async Task CancelProductionEntry_ShouldReverseNetInventoryAndKeepEntry()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);
        var confirm = new ConfirmProductionEntryHandler(db, new ConfirmProductionEntryValidator());

        var confirmed = await confirm.HandleAsync(new ConfirmProductionEntryCommand(
            null,
            fixture.BusinessId,
            fixture.BranchId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            [new ConfirmProductionEntryLine(fixture.ProductId, 10m, 280m, 1m)]));
        confirmed.IsSuccess.Should().BeTrue();

        var cancel = new CancelProductionEntryHandler(db, new CancelProductionEntryValidator());
        var result = await cancel.HandleAsync(new CancelProductionEntryCommand(
            confirmed.Value.ProductionEntryId,
            "Error de conteo"));

        result.IsSuccess.Should().BeTrue();

        var entry = await db.ProductionEntries.SingleAsync(e => e.Id == confirmed.Value.ProductionEntryId);
        entry.Status.Should().Be(ProductionEntryStatus.Cancelled);

        var stock = await db.InventoryStocks.SingleAsync(s => s.ProductId == fixture.ProductId);
        stock.Quantity.Should().Be(5m);

        var reverse = await db.InventoryMovements.SingleAsync(m => m.MovementType == MovementType.ProductionCancellation);
        reverse.Quantity.Should().Be(9m);
        reverse.ReferenceId.Should().Be(entry.Id);
    }

    [Fact]
    public async Task CancelProductionEntry_ShouldRestoreConsumedInputs()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);
        var confirm = new ConfirmProductionEntryHandler(db, new ConfirmProductionEntryValidator());

        var confirmed = await confirm.HandleAsync(new ConfirmProductionEntryCommand(
            null,
            fixture.BusinessId,
            fixture.BranchId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            [new ConfirmProductionEntryLine(fixture.ProductId, 10m, 280m)],
            Inputs: [new ConfirmProductionInputLine(fixture.InputProductId, 2.5m)]));
        confirmed.IsSuccess.Should().BeTrue();

        var cancel = new CancelProductionEntryHandler(db, new CancelProductionEntryValidator());
        var result = await cancel.HandleAsync(new CancelProductionEntryCommand(
            confirmed.Value.ProductionEntryId,
            "Error de receta"));

        result.IsSuccess.Should().BeTrue();

        var inputStock = await db.InventoryStocks.SingleAsync(s => s.ProductId == fixture.InputProductId);
        inputStock.Quantity.Should().Be(20m);

        var cancellationMovements = await db.InventoryMovements
            .Where(m => m.MovementType == MovementType.ProductionCancellation)
            .ToListAsync();
        cancellationMovements.Should().Contain(m => m.ProductId == fixture.InputProductId && m.Quantity == 2.5m);
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

    private sealed record FixtureData(
        Guid BusinessId,
        Guid BranchId,
        Guid ProductId,
        Guid SecondProductId,
        Guid InputProductId);
}
