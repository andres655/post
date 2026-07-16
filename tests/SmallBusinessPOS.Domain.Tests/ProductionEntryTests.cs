using FluentAssertions;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Domain.Tests;

public class ProductionEntryTests
{
    [Fact]
    public void Confirm_ShouldSetConfirmedStatus()
    {
        var entry = ProductionEntry.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "PROD-20260716-0001",
            DateOnly.FromDateTime(DateTime.UtcNow));

        entry.AddDetail(ProductionEntryDetail.Create(entry.Id, Guid.CreateVersion7(), 10m, 100m));

        entry.Confirm("supervisor@pollosaboroso.local");

        entry.Status.Should().Be(ProductionEntryStatus.Confirmed);
        entry.ConfirmedAtUtc.Should().NotBeNull();
        entry.ConfirmedBy.Should().Be("supervisor@pollosaboroso.local");
    }

    [Fact]
    public void Confirm_ShouldThrow_WhenAlreadyConfirmed()
    {
        var entry = ProductionEntry.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "PROD-20260716-0001",
            DateOnly.FromDateTime(DateTime.UtcNow));

        entry.AddDetail(ProductionEntryDetail.Create(entry.Id, Guid.CreateVersion7(), 10m));
        entry.Confirm();

        var act = () => entry.Confirm();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Detail_ShouldRejectNonPositiveQuantity()
    {
        var act = () => ProductionEntryDetail.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            0m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Detail_ShouldRejectWasteGreaterThanProduced()
    {
        var act = () => ProductionEntryDetail.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            10m,
            quantityWasted: 11m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
