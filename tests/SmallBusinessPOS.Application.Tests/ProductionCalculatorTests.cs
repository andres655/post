using FluentAssertions;
using SmallBusinessPOS.Application.Features.Production.Calculations;

namespace SmallBusinessPOS.Application.Tests;

public class ProductionCalculatorTests
{
    private readonly ProductionCalculator _calculator = new();

    [Fact]
    public void CalculateLine_ShouldCalculateNetAndDirectCost()
    {
        var result = _calculator.CalculateLine(new ProductionLineInput(
            QuantityProduced: 12m,
            QuantityWasted: 1.5m,
            UnitCost: 125.255m));

        result.NetQuantity.Should().Be(10.5m);
        result.DirectCost.Should().Be(1503.06m);
    }

    [Fact]
    public void CalculateLine_ShouldClampNetQuantityAtZero()
    {
        var result = _calculator.CalculateLine(new ProductionLineInput(
            QuantityProduced: 2m,
            QuantityWasted: 5m,
            UnitCost: 100m));

        result.NetQuantity.Should().Be(0m);
        result.DirectCost.Should().Be(200m);
    }

    [Fact]
    public void CalculateFormSummary_ShouldAggregateLines()
    {
        var summary = _calculator.CalculateFormSummary(
        [
            new ProductionLineInput(10m, 1m, 200m),
            new ProductionLineInput(4.5m, 0.5m, 120m)
        ]);

        summary.TotalProduced.Should().Be(14.5m);
        summary.TotalWasted.Should().Be(1.5m);
        summary.NetQuantity.Should().Be(13m);
        summary.DirectCost.Should().Be(2540m);
    }

    [Fact]
    public void CalculateHistorySummary_ShouldAggregateHistoryTotals()
    {
        var summary = _calculator.CalculateHistorySummary(
        [
            new ProductionHistoryTotalsInput(10m, 1m, 9m, 1800m),
            new ProductionHistoryTotalsInput(4m, 0m, 4m, 720.126m)
        ]);

        summary.TotalProduced.Should().Be(14m);
        summary.TotalWasted.Should().Be(1m);
        summary.NetQuantity.Should().Be(13m);
        summary.DirectCost.Should().Be(2520.13m);
    }

    [Fact]
    public void CalculateExpectedMarginPercent_ShouldUseNetQuantityAndDirectCost()
    {
        var margin = _calculator.CalculateExpectedMarginPercent(new ProductionMarginInput(
            QuantityProduced: 10m,
            QuantityWasted: 1m,
            UnitCost: 280m,
            SalePrice: 650m));

        margin.Should().Be(52.14m);
    }

    [Fact]
    public void CalculateExpectedMarginPercent_ShouldReturnZero_WhenSalesAreNotPositive()
    {
        var margin = _calculator.CalculateExpectedMarginPercent(new ProductionMarginInput(
            QuantityProduced: 10m,
            QuantityWasted: 10m,
            UnitCost: 280m,
            SalePrice: 650m));

        margin.Should().Be(0m);
    }
}
