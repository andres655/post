namespace SmallBusinessPOS.Application.Features.Customers.CreateCustomer;

public sealed record CreateCustomerCommand(
    Guid BusinessId,
    string Name,
    string? DocumentNumber = null,
    string? Phone = null,
    string? Email = null);
