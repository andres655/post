namespace SmallBusinessPOS.Application.Features.Customers.GetCustomers;

public sealed record GetCustomersQuery(Guid BusinessId, string? SearchTerm = null, int MaxRows = 100);
