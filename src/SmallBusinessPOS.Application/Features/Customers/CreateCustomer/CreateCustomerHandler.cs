using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.Customers;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Application.Features.Customers.CreateCustomer;

public sealed class CreateCustomerHandler(
    IAppDbContext db,
    CreateCustomerValidator validator)
{
    public async Task<Result<CustomerDto>> HandleAsync(CreateCustomerCommand command, CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var error = validation.Errors[0];
            return Result.Failure<CustomerDto>(Error.Validation(error.PropertyName, error.ErrorMessage));
        }

        var customer = Customer.Create(
            command.BusinessId,
            command.Name,
            command.DocumentNumber,
            command.Phone,
            command.Email);

        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);

        return Result.Success(new CustomerDto(
            customer.Id,
            customer.Name,
            customer.DocumentNumber,
            customer.Phone,
            customer.Email));
    }
}
