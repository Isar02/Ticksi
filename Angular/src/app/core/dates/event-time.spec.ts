import { eventLocalNow, eventWallClockValue } from './event-time';

describe('event clock', () => {
  it('uses the Sarajevo calendar day at midnight, regardless of the browser zone', () => {
    expect(eventLocalNow(new Date('2026-10-05T22:30:00.123Z'))).toBe('2026-10-06T00:30:00.123');
  });

  it('follows Sarajevo summer and winter offsets across the clock change', () => {
    expect(eventLocalNow(new Date('2026-10-25T00:30:00Z'))).toBe('2026-10-25T02:30:00.000');
    expect(eventLocalNow(new Date('2026-10-25T01:30:00Z'))).toBe('2026-10-25T02:30:00.000');
    expect(eventLocalNow(new Date('2026-12-01T10:00:00Z'))).toBe('2026-12-01T11:00:00.000');
  });

  it('compares event times as wall clocks, including times skipped by the browser DST', () => {
    expect(eventWallClockValue('2026-03-08T03:00:00') - eventWallClockValue('2026-03-08T02:30:00')).toBe(30 * 60_000);
  });
});
