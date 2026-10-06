using Microsoft.Extensions.Options;
using Ticksi.Application.Interfaces;
using Ticksi.Application.Options;
using Ticksi.Infrastructure.Services;

namespace Ticksi.Tests.Common;

public static class EventClocks
{
    public static IEventClock In(string timeZone, TimeProvider time) =>
        new EventClock(time, Options.Create(new EventOptions { TimeZone = timeZone }));

    public static IEventClock Utc(TimeProvider time) => In("UTC", time);
}
