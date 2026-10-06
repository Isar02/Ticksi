import { summarize } from './event-summary';
import { createEventWizardForm } from './event-wizard-form';

describe('event summary time', () => {
  it('keeps every accepted single-digit hour ready for display as a wall-clock time', () => {
    const form = createEventWizardForm(() => null);
    for (const time of ['9:30', ' 9:30 ', '09:30']) {
      form.controls.schedule.patchValue({ date: '2026-11-14', time });
      const summary = summarize(form, { categories: [], eventTypes: [], organizerCompanies: [], venues: [] });
      expect(summary.startsAt).withContext(time).toEqual({ day: 14, month: 11, year: 2026, time: '09:30' });
    }
  });

  it('keeps the displayed time even when the browser DST skips that hour', () => {
    const form = createEventWizardForm(() => null);
    form.controls.schedule.patchValue({ date: '2026-03-08', time: '2:30' });
    expect(summarize(form, { categories: [], eventTypes: [], organizerCompanies: [], venues: [] }).startsAt)
      .toEqual({ day: 8, month: 3, year: 2026, time: '02:30' });
  });

  it('leaves the live preview date empty while the time is incomplete or invalid', () => {
    const form = createEventWizardForm(() => null);
    for (const time of ['9:', '25:30', '  ']) {
      form.controls.schedule.patchValue({ date: '2026-11-14', time });
      expect(summarize(form, { categories: [], eventTypes: [], organizerCompanies: [], venues: [] }).startsAt).toBeNull();
    }
  });
});
