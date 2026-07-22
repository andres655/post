using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Tests;

internal sealed class TestClock(DateTime? utcNow = null) : IClock
{
    public DateTime UtcNow { get; } = utcNow ?? new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc);
    public DateTimeOffset UtcNowOffset => new(UtcNow);
    public DateOnly TodayUtc => DateOnly.FromDateTime(UtcNow);
}
