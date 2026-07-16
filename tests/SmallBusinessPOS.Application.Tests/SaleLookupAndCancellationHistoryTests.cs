using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Features.Sales.GetCancellationHistory;
using SmallBusinessPOS.Application.Features.Sales.GetSaleByNumber;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Tests;

public class SaleLookupAndCancellationHistoryTests
{
    private static IAppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task GetSaleByNumber_ShouldFindSale_WhenNumberHasDifferentCasingAndSpaces()
    {
        var db = CreateDb();
        var fixture = await SeedCancelledSaleAsync(db);
        var handler = new GetSaleByNumberHandler(db);

        var result = await handler.HandleAsync(new GetSaleByNumberQuery(
            fixture.BusinessId,
            "  prin-c01-20260716-000025  "));

        result.IsSuccess.Should().BeTrue();
        result.Value.SaleId.Should().Be(fixture.SaleId);
        result.Value.Number.Should().Be("PRIN-C01-20260716-000025");
        result.Value.Status.Should().Be(SaleStatus.Cancelled.ToString());
    }

    [Fact]
    public async Task GetSaleByNumber_ShouldFail_WhenNumberDoesNotExist()
    {
        var db = CreateDb();
        var fixture = await SeedCancelledSaleAsync(db);
        var handler = new GetSaleByNumberHandler(db);

        var result = await handler.HandleAsync(new GetSaleByNumberQuery(
            fixture.BusinessId,
            "PRIN-C01-20260716-999999"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sale.NotFound");
    }

    [Fact]
    public async Task GetCancellationHistory_ShouldReturnCancelledSalesForBranchAndDateRange()
    {
        var db = CreateDb();
        var fixture = await SeedCancelledSaleAsync(db);
        var otherBranch = Branch.Create(fixture.BusinessId, "Otra sucursal");
        var otherSale = BuildCancelledSale(fixture.BusinessId, otherBranch.Id, "PRIN-C02-20260716-000001");

        db.Branches.Add(otherBranch);
        db.Sales.Add(otherSale);
        await db.SaveChangesAsync();

        var handler = new GetCancellationHistoryHandler(db);
        var result = await handler.HandleAsync(new GetCancellationHistoryQuery(
            fixture.BusinessId,
            fixture.BranchId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].SaleId.Should().Be(fixture.SaleId);
        result.Value[0].Reason.Should().Be("Error de digitacion");
    }

    [Fact]
    public async Task GetCancellationHistory_ShouldValidateDateRange()
    {
        var db = CreateDb();
        var handler = new GetCancellationHistoryHandler(db);

        var result = await handler.HandleAsync(new GetCancellationHistoryQuery(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Validation.ToDate");
    }

    private static async Task<SaleFixture> SeedCancelledSaleAsync(IAppDbContext db)
    {
        var business = Business.Create("Pollo Sabroso", "DOP", "America/Santo_Domingo", BusinessType.RotisserieChicken);
        var branch = Branch.Create(business.Id, "Sucursal Principal", isMain: true);
        var sale = BuildCancelledSale(business.Id, branch.Id, "PRIN-C01-20260716-000025");

        db.Businesses.Add(business);
        db.Branches.Add(branch);
        db.Sales.Add(sale);
        await db.SaveChangesAsync();

        return new SaleFixture(business.Id, branch.Id, sale.Id);
    }

    private static Sale BuildCancelledSale(Guid businessId, Guid branchId, string number)
    {
        var productId = Guid.CreateVersion7();
        var paymentMethodId = Guid.CreateVersion7();

        var sale = Sale.Create(businessId, branchId, number, SaleType.Counter);
        sale.AddDetail(SaleDetail.Create(sale.Id, productId, "POL-ENT", "Pollo entero", 1m, 650m));
        sale.ApplyFinancials(0m, 0m);
        sale.AddPayment(SalePayment.Create(sale.Id, paymentMethodId, 650m));
        sale.Confirm("cashier@pollosaboroso.local");
        sale.Cancel("Error de digitacion", "supervisor@pollosaboroso.local");

        return sale;
    }

    private sealed record SaleFixture(Guid BusinessId, Guid BranchId, Guid SaleId);
}
