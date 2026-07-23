namespace SmallBusinessPOS.Application.Features.Production.Calculations;

public sealed class ProductionCalculator
{
    public ProductionLineCalculation CalculateLine(ProductionLineInput line)
    {
        var netQuantity = Math.Max(0m, line.QuantityProduced - line.QuantityWasted);
        var directCost = line.QuantityProduced * line.UnitCost;

        return new ProductionLineCalculation(
            Round(netQuantity),
            Round(directCost));
    }

    public ProductionSummary CalculateFormSummary(IReadOnlyCollection<ProductionLineInput> lines)
    {
        var lineCalculations = lines.Select(CalculateLine).ToList();

        return new ProductionSummary(
            Round(lines.Sum(line => line.QuantityProduced)),
            Round(lines.Sum(line => line.QuantityWasted)),
            Round(lineCalculations.Sum(line => line.NetQuantity)),
            Round(lineCalculations.Sum(line => line.DirectCost)));
    }

    public ProductionSummary CalculateHistorySummary(IReadOnlyCollection<ProductionHistoryTotalsInput> entries) =>
        new(
            Round(entries.Sum(entry => entry.TotalProduced)),
            Round(entries.Sum(entry => entry.TotalWasted)),
            Round(entries.Sum(entry => entry.NetAdded)),
            Round(entries.Sum(entry => entry.TotalCost)));

    public decimal CalculateExpectedMarginPercent(ProductionMarginInput input)
    {
        if (input.SalePrice <= 0m || input.QuantityProduced <= 0m)
            return 0m;

        var line = CalculateLine(new ProductionLineInput(
            input.QuantityProduced,
            input.QuantityWasted,
            input.UnitCost));

        var sales = line.NetQuantity * input.SalePrice;
        if (sales <= 0m)
            return 0m;

        return Round(((sales - line.DirectCost) / sales) * 100m);
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

public sealed record ProductionLineInput(
    decimal QuantityProduced,
    decimal QuantityWasted,
    decimal UnitCost);

public sealed record ProductionHistoryTotalsInput(
    decimal TotalProduced,
    decimal TotalWasted,
    decimal NetAdded,
    decimal TotalCost);

public sealed record ProductionMarginInput(
    decimal QuantityProduced,
    decimal QuantityWasted,
    decimal UnitCost,
    decimal SalePrice);

public sealed record ProductionLineCalculation(
    decimal NetQuantity,
    decimal DirectCost);

public sealed record ProductionSummary(
    decimal TotalProduced,
    decimal TotalWasted,
    decimal NetQuantity,
    decimal DirectCost);
