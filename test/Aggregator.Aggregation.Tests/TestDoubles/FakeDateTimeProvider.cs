using Aggregator.Common.Application.Clock;

namespace Aggregator.Aggregation.Tests.TestDoubles;

/// <summary>Controllable clock for deterministic time-based tests.</summary>
internal sealed class FakeDateTimeProvider(DateTime start) : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = start;

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}
