using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.PaymentMethods.GetActivePaymentMethods;

public sealed record PaymentMethodDto(Guid Id, string Code, string Name, PaymentMethodType Type);
