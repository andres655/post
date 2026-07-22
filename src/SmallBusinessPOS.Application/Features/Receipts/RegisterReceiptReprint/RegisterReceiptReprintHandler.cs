using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Application.Features.Receipts.RegisterReceiptReprint;

public sealed class RegisterReceiptReprintHandler(IAppDbContext db)
{
    public async Task<Result> HandleAsync(
        RegisterReceiptReprintCommand command,
        CancellationToken ct = default)
    {
        db.ReceiptReprintAudits.Add(ReceiptReprintAudit.Create(
            command.BusinessId,
            command.BranchId,
            command.SaleId,
            command.ReceiptNumber,
            command.ReprintedBy,
            command.LookupMethod));

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
