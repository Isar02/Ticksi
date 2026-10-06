namespace Ticksi.Application.Interfaces;

// Event dates are wall-clock times where the events take place, so they are compared with this clock, not with UTC.
public interface IEventClock
{
    DateTime Now { get; }
}
