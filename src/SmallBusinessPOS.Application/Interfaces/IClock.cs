namespace SmallBusinessPOS.Application.Interfaces;

public interface IClock
{
    DateTime UtcNow { get; }
    DateTimeOffset UtcNowOffset { get; }
    DateOnly TodayUtc { get; }
}
