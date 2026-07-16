using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.PaymentMethods.GetActivePaymentMethods;

public sealed class GetActivePaymentMethodsHandler(IAppDbContext db)
{
    public async Task<Result<List<PaymentMethodDto>>> HandleAsync(
        GetActivePaymentMethodsQuery query,
        CancellationToken ct = default)
    {
        var methods = await db.PaymentMethods
            .Where(pm => pm.BusinessId == query.BusinessId && pm.IsActive)
            .OrderBy(pm => pm.Name)
            .Select(pm => new PaymentMethodDto(pm.Id, pm.Code, pm.Name, pm.Type))
            .ToListAsync(ct);

        return Result.Success(methods);
    }
}
