namespace SmallBusinessPOS.Application.Interfaces;

public interface IReceiptService
{
    Task<byte[]> GenerateSaleReceiptAsync(Guid saleId, CancellationToken cancellationToken);
}
