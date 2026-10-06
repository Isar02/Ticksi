using Microsoft.Extensions.Time.Testing;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.EventTests.UnitTests;

public class EventClockTests
{
    [Theory]
    [InlineData(2026, 7, 1, 18, 30, 20)]
    [InlineData(2026, 12, 1, 18, 30, 19)]
    public void Now_IsTheWallClockTimeInTheConfiguredZone(int year, int month, int day, int utcHour, int minute, int localHour)
    {
        var utc = new DateTimeOffset(year, month, day, utcHour, minute, 0, TimeSpan.Zero);

        var now = EventClocks.In("Europe/Sarajevo", new FakeTimeProvider(utc)).Now;

        Assert.Equal(new DateTime(year, month, day, localHour, minute, 0), now);
        Assert.Equal(DateTimeKind.Unspecified, now.Kind);
    }
}
