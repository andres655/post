using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Categories.DisableCategory;

public sealed class DisableCategoryHandler(IAppDbContext db)
{
    public async Task<Result> HandleAsync(
        DisableCategoryCommand command,
        string? currentUser = null,
        CancellationToken ct = default)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Id == command.Id, ct);

        if (category is null)
            return Result.Failure(Error.NotFound("Category", command.Id));

        if (!category.IsActive)
            return Result.Failure(
                Error.BusinessRule("Category.AlreadyDisabled", "La categoría ya está deshabilitada."));

        category.Disable();

        if (currentUser is not null)
            category.SetUpdated(currentUser);

        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
