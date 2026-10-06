using Microsoft.Extensions.Options;
using Ticksi.Application.Interfaces;
using Ticksi.Application.Options;

namespace Ticksi.Infrastructure.Services;

public sealed class EventClock : IEventClock
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;

    public EventClock(TimeProvider timeProvider, IOptions<EventOptions> options)
    {
        _timeProvider = timeProvider;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);
    }

    public DateTime Now => TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), _timeZone).DateTime;
}
