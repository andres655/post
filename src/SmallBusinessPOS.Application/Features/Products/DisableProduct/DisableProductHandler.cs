using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Products.DisableProduct;

public sealed class DisableProductHandler(IAppDbContext db)
{
    public async Task<Result> HandleAsync(
        DisableProductCommand command,
        string? currentUser = null,
        CancellationToken ct = default)
    {
        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == command.Id, ct);

        if (product is null)
            return Result.Failure(Error.NotFound("Product", command.Id));

        if (!product.IsActive)
            return Result.Failure(
                Error.BusinessRule("Product.AlreadyDisabled", "El producto ya está deshabilitado."));

        product.Disable();

        if (currentUser is not null)
            product.SetUpdated(currentUser);

        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
