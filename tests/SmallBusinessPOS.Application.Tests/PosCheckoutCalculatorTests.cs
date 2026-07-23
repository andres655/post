using FluentAssertions;
using SmallBusinessPOS.Application.Features.POS.Checkout;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Tests;

public sealed class PosCheckoutCalculatorTests
{
    private readonly PosCheckoutCalculator _calculator = new();

    [Fact]
    public void CalculateSubtotal_ShouldUseServerCartInputs()
    {
        var subtotal = _calculator.CalculateSubtotal(
        [
            new PosCartLineInput(2m, 125.125m),
            new PosCartLineInput(0.5m, 100m)
        ]);

        subtotal.Should().Be(300.25m);
    }

    [Fact]
    public void CalculateTotal_ShouldNormalizeDiscountAndClampAtZero()
    {
        _calculator.CalculateTotal(100m, -20m, 18m).Should().Be(118m);
        _calculator.CalculateTotal(100m, 150m, 0m).Should().Be(0m);
    }

    [Fact]
    public void CalculatePaidTotal_ShouldIgnoreZeroOrNegativePaymentInputs()
    {
        var paid = _calculator.CalculatePaidTotal(
        [
            Payment(PaymentMethodType.Cash, 100m),
            Payment(PaymentMethodType.DebitCard, 50m),
            Payment(PaymentMethodType.Cash, -10m)
        ]);

        paid.Should().Be(150m);
    }

    [Fact]
    public void CalculateCashChange_ShouldReturnChangeOnlyFromCash()
    {
        var change = _calculator.CalculateCashChange(
            100m,
            [
                Payment(PaymentMethodType.Cash, 120m),
                Payment(PaymentMethodType.DebitCard, 50m, "AUTH-1")
            ]);

        change.Should().Be(20m);
    }

    [Fact]
    public void CalculateCashChange_ShouldIgnoreOverpaymentWithoutCash()
    {
        var change = _calculator.CalculateCashChange(
            100m,
            [
                Payment(PaymentMethodType.DebitCard, 150m, "AUTH-1")
            ]);

        change.Should().Be(0m);
    }

    private static PosPaymentInput Payment(
        PaymentMethodType type,
        decimal amount,
        string? reference = null) =>
        new(Guid.CreateVersion7(), type == PaymentMethodType.Cash ? "CASH" : "CARD", type.ToString(), type, amount, reference);
}
