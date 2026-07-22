using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.Customers;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Customers.GetCustomers;

public sealed class GetCustomersHandler(IAppDbContext db)
{
    public async Task<Result<List<CustomerDto>>> HandleAsync(GetCustomersQuery query, CancellationToken ct = default)
    {
        var customers = db.Customers
            .Where(customer => customer.BusinessId == query.BusinessId && customer.IsActive);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim().ToLower();
            customers = customers.Where(customer =>
                customer.Name.ToLower().Contains(term)
                || (customer.DocumentNumber != null && customer.DocumentNumber.ToLower().Contains(term))
                || (customer.Phone != null && customer.Phone.ToLower().Contains(term)));
        }

        var take = Math.Clamp(query.MaxRows, 1, 200);
        var result = await customers
            .OrderBy(customer => customer.Name)
            .Take(take)
            .Select(customer => new CustomerDto(
                customer.Id,
                customer.Name,
                customer.DocumentNumber,
                customer.Phone,
                customer.Email))
            .ToListAsync(ct);

        return Result.Success(result);
    }
}
