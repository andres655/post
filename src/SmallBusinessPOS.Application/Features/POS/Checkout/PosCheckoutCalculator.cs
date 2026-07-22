using SmallBusinessPOS.Application.Features.Sales.CreateSale;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.POS.Checkout;

public sealed class PosCheckoutCalculator
{
    public decimal CalculateTax(decimal subtotal, decimal discount, bool usesTaxes, decimal defaultTaxRate)
    {
        if (!usesTaxes || defaultTaxRate <= 0)
            return 0m;

        var taxableBase = Math.Max(0m, subtotal - Math.Max(0m, discount));
        return Math.Round(taxableBase * (defaultTaxRate / 100m), 2, MidpointRounding.AwayFromZero);
    }

    public PosCheckoutPaymentResult BuildSalePayments(
        decimal total,
        IReadOnlyCollection<PosPaymentInput> paymentInputs)
    {
        var enteredPayments = paymentInputs
            .Where(payment => payment.Amount > 0)
            .Select(payment => new PosPaymentSnapshot(
                payment.PaymentMethodId,
                payment.Code,
                payment.Name,
                payment.Type,
                Math.Round(payment.Amount, 2, MidpointRounding.AwayFromZero),
                Math.Round(payment.Amount, 2, MidpointRounding.AwayFromZero),
                payment.Reference))
            .ToList();

        var roundedTotal = Math.Round(total, 2, MidpointRounding.AwayFromZero);
        var paid = enteredPayments.Sum(payment => payment.Amount);
        if (paid < roundedTotal)
        {
            return PosCheckoutPaymentResult.Failure(
                $"Faltan {roundedTotal - paid:N2} para cubrir el total de la venta.");
        }

        var overpaid = paid - roundedTotal;
        if (overpaid > 0m)
        {
            var cashPayment = enteredPayments
                .FirstOrDefault(payment => IsCashPayment(payment) && payment.Amount >= overpaid);
            if (cashPayment is null)
            {
                return PosCheckoutPaymentResult.Failure(
                    "El pago excede el total. Solo se puede devolver cambio desde efectivo.");
            }

            cashPayment.Amount -= overpaid;
        }

        var payments = enteredPayments
            .Where(payment => payment.Amount > 0)
            .Select(payment => new CreateSalePayment(
                payment.PaymentMethodId,
                payment.Amount,
                payment.Reference,
                payment.TenderedAmount))
            .ToList();

        return PosCheckoutPaymentResult.Success(payments);
    }

    private static bool IsCashPayment(PosPaymentSnapshot payment) =>
        payment.Type == PaymentMethodType.Cash
        || payment.Code.Equals("CASH", StringComparison.OrdinalIgnoreCase)
        || payment.Code.Equals("EFECTIVO", StringComparison.OrdinalIgnoreCase)
        || payment.Name.Contains("efectivo", StringComparison.OrdinalIgnoreCase)
        || payment.Name.Contains("cash", StringComparison.OrdinalIgnoreCase);
}

public sealed record PosPaymentInput(
    Guid PaymentMethodId,
    string Code,
    string Name,
    PaymentMethodType Type,
    decimal Amount,
    string? Reference = null);

public sealed class PosCheckoutPaymentResult
{
    private PosCheckoutPaymentResult(bool isSuccess, List<CreateSalePayment> payments, string? error)
    {
        IsSuccess = isSuccess;
        Payments = payments;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public List<CreateSalePayment> Payments { get; }
    public string? Error { get; }

    public static PosCheckoutPaymentResult Success(List<CreateSalePayment> payments) =>
        new(true, payments, null);

    public static PosCheckoutPaymentResult Failure(string error) =>
        new(false, [], error);
}

internal sealed class PosPaymentSnapshot(
    Guid paymentMethodId,
    string code,
    string name,
    PaymentMethodType type,
    decimal amount,
    decimal tenderedAmount,
    string? reference)
{
    public Guid PaymentMethodId { get; } = paymentMethodId;
    public string Code { get; } = code;
    public string Name { get; } = name;
    public PaymentMethodType Type { get; } = type;
    public decimal Amount { get; set; } = amount;
    public decimal TenderedAmount { get; } = tenderedAmount;
    public string? Reference { get; } = reference;
}
