namespace SmallBusinessPOS.Application.Features.Customers;

public sealed record CustomerDto(
    Guid Id,
    string Name,
    string? DocumentNumber,
    string? Phone,
    string? Email);
