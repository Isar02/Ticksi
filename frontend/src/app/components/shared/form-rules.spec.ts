import { FormControl, FormGroup } from '@angular/forms';
import { endDateNotBeforeStart, trimmedPattern } from './form-rules';

describe('form rules', () => {
  it('flags a range that ends before it starts, for any pair of fields', () => {
    const group = new FormGroup(
      { registeredFrom: new FormControl('2026-10-31'), registeredTo: new FormControl('2026-10-01') },
      { validators: endDateNotBeforeStart('registeredFrom', 'registeredTo') }
    );
    expect(group.hasError('dateRange')).toBeTrue();

    group.patchValue({ registeredTo: '2026-10-31' });
    expect(group.hasError('dateRange')).toBeFalse();
  });

  it('leaves a date that could not be read to its own field', () => {
    const group = new FormGroup(
      { dateFrom: new FormControl('invalid'), dateTo: new FormControl('2026-10-01') },
      { validators: endDateNotBeforeStart() }
    );

    expect(group.hasError('dateRange')).toBeFalse();
  });

  it('checks a pattern on the trimmed value', () => {
    const phone = new FormControl(' +387 61 123 456 ', { validators: trimmedPattern(/^\+?[0-9\s-]{9,}$/) });
    expect(phone.valid).toBeTrue();

    phone.setValue('12ab');
    expect(phone.hasError('pattern')).toBeTrue();
  });
});
