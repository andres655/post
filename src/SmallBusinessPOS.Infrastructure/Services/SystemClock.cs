using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;
    public DateOnly TodayUtc => DateOnly.FromDateTime(UtcNow);
}
