namespace SmallBusinessPOS.Application.Interfaces;

public interface ICurrentUserService
{
    Task<string?> GetUserNameAsync(CancellationToken cancellationToken = default);
    Task<string?> GetUserIdAsync(CancellationToken cancellationToken = default);
    Task<bool> IsInRoleAsync(string role, CancellationToken cancellationToken = default);
}
