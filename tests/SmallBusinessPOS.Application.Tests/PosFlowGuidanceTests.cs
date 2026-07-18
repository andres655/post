using FluentAssertions;
using SmallBusinessPOS.Web.Components.Pages;

namespace SmallBusinessPOS.Application.Tests;

public class PosFlowGuidanceTests
{
    [Fact]
    public void CanAttemptCheckout_ReturnsFalse_WhenNoOpenSessionOrProducts()
    {
        PosFlowGuidance.CanAttemptCheckout(false, true, 100m, 0m).Should().BeFalse();
        PosFlowGuidance.CanAttemptCheckout(true, false, 100m, 0m).Should().BeFalse();
    }

    [Fact]
    public void CanAttemptCheckout_ReturnsTrue_WhenPaymentHasBeenEntered()
    {
        PosFlowGuidance.CanAttemptCheckout(true, true, 100m, 10m).Should().BeTrue();
    }

    [Fact]
    public void CanConfirmCheckout_ReturnsTrue_WhenTotalIsCovered()
    {
        PosFlowGuidance.CanConfirmCheckout(true, true, 100m, 100m).Should().BeTrue();
    }
}
