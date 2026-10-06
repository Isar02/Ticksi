import { environment } from '../../../environments/environment';

const TIME_OF_DAY = /^([01]?\d|2[0-3]):[0-5]\d$/;

export function normalizeEventTime(time: string): string | null {
  const trimmed = time.trim();
  return TIME_OF_DAY.test(trimmed) ? trimmed.padStart(5, '0') : null;
}

// Match Events:TimeZone on the API. Event dates are wall-clock values, not UTC instants.
const eventClock = new Intl.DateTimeFormat('en-GB', {
  timeZone: environment.eventTimeZone,
  year: 'numeric', month: '2-digit', day: '2-digit',
  hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23'
});

export function eventLocalNow(now: Date = new Date()): string {
  const parts = eventClock.formatToParts(now);
  const part = (type: Intl.DateTimeFormatPartTypes) => parts.find(value => value.type === type)!.value;
  return `${part('year')}-${part('month')}-${part('day')}T${part('hour')}:${part('minute')}:${part('second')}.${String(now.getUTCMilliseconds()).padStart(3, '0')}`;
}

// A UTC surrogate lets us compare wall clocks without the browser's time zone or daylight saving shifts.
export function eventWallClockValue(dateTime: string): number {
  return Date.parse(`${dateTime}Z`);
}
