using FluentAssertions;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Domain.Tests;

public class ReceiptReprintAuditTests
{
    [Fact]
    public void Create_ShouldNormalizeSaleNumber_AndStoreAuditData()
    {
        var businessId = Guid.CreateVersion7();
        var branchId = Guid.CreateVersion7();
        var saleId = Guid.CreateVersion7();

        var audit = ReceiptReprintAudit.Create(
            businessId,
            branchId,
            saleId,
            " prin-c01-20260716-000025 ",
            " cashier@pollosaboroso.local ",
            "SaleNumber");

        audit.BusinessId.Should().Be(businessId);
        audit.BranchId.Should().Be(branchId);
        audit.SaleId.Should().Be(saleId);
        audit.SaleNumber.Should().Be("PRIN-C01-20260716-000025");
        audit.ReprintedBy.Should().Be("cashier@pollosaboroso.local");
        audit.Source.Should().Be("SaleNumber");
        audit.ReprintedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_ShouldRejectBlankActor()
    {
        var act = () => ReceiptReprintAudit.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "PRIN-C01-20260716-000025",
            " ",
            "SaleNumber");

        act.Should().Throw<ArgumentException>();
    }
}
