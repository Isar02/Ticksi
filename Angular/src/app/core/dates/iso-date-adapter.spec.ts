import { IsoDateAdapter } from './iso-date-adapter';

describe('IsoDateAdapter', () => {
  const adapter = new IsoDateAdapter();

  it('reads typed regional dates into yyyy-MM-dd and refuses impossible ones', () => {
    expect(adapter.parse('5. 10. 2026.')).toBe('2026-10-05');
    expect(adapter.parse('05.10.2026')).toBe('2026-10-05');
    expect(adapter.parse(' 2026-12-31 ')).toBe('2026-12-31');
    expect(adapter.parse('')).toBeNull();

    for (const typed of ['31. 2. 2026.', '10/05/2026', 'tomorrow', '2026-02-30']) {
      expect(adapter.isValid(adapter.parse(typed)!)).withContext(typed).toBeFalse();
    }
  });

  it('shows dates as the app does and names months in English', () => {
    expect(adapter.format('2026-10-05', 'input')).toBe('5. 10. 2026.');
    expect(adapter.format('2026-10-05', 'month-year')).toBe('Oct 2026');
    expect(adapter.format('2026-10-05', 'long')).toBe('5 October 2026');
    expect(adapter.getMonthNames('long')[0]).toBe('January');
  });

  it('starts the week on Monday and lists day names from Sunday', () => {
    expect(adapter.getFirstDayOfWeek()).toBe(1);
    expect(adapter.getDayOfWeekNames('short').slice(0, 2)).toEqual(['Sun', 'Mon']);
    expect(adapter.getDayOfWeek('2026-10-05')).toBe(1);
  });

  it('moves through the calendar without leaving the month', () => {
    expect(adapter.addCalendarMonths('2026-01-31', 1)).toBe('2026-02-28');
    expect(adapter.addCalendarYears('2028-02-29', 1)).toBe('2029-02-28');
    expect(adapter.addCalendarDays('2026-12-31', 1)).toBe('2027-01-01');
    expect(adapter.getNumDaysInMonth('2028-02-10')).toBe(29);
  });

  it('takes empty form values as no date and anything else unreadable as invalid', () => {
    expect(adapter.deserialize('')).toBeNull();
    expect(adapter.deserialize(null)).toBeNull();
    expect(adapter.deserialize('2026-10-05')).toBe('2026-10-05');
    expect(adapter.isValid(adapter.deserialize('5. 10. 2026.')!)).toBeFalse();
  });
});
