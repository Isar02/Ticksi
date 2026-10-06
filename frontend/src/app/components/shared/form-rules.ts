import { AbstractControl, FormGroup, ValidationErrors, ValidatorFn } from '@angular/forms';
import { isIsoDate } from '../../core/dates/iso-date-adapter';

// FluentValidation's NotEmpty rejects whitespace-only strings; Angular's required does not.
export function requiredText(control: AbstractControl<string>): ValidationErrors | null {
  return control.value?.trim() ? null : { required: true };
}

// Same check as the API's default FluentValidation EmailAddress validator, on the value sent to it.
export function apiEmail(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value?.trim() ?? '';
  if (!value) return null;

  const at = value.indexOf('@');
  return at > 0 && at < value.length - 1 && at === value.lastIndexOf('@') ? null : { email: true };
}

// Checks the value as it is sent, without the spaces around it.
export function trimmedPattern(pattern: RegExp): ValidatorFn {
  return (control: AbstractControl<string>): ValidationErrors | null => {
    const value = control.value?.trim() ?? '';
    return value && !pattern.test(value) ? { pattern: true } : null;
  };
}

export function trimmedMinLength(requiredLength: number): ValidatorFn {
  return (control: AbstractControl<string>): ValidationErrors | null => {
    const value = control.value ?? '';
    if (!value) return null;
    if (!value.trim()) return { required: true };
    return value.trim().length < requiredLength ? { trimmedMinLength: { requiredLength } } : null;
  };
}

// Puts each API field error on its control and returns the messages that fit no field.
export function applyServerErrors(form: FormGroup, fieldErrors: Readonly<Record<string, string[]>>): string[] {
  const unplaced: string[] = [];

  for (const [field, messages] of Object.entries(fieldErrors)) {
    const key = field.replace(/^\$\./, '').toLowerCase();
    const control = Object.entries(form.controls).find(([name]) => name.toLowerCase() === key)?.[1];

    if (control && control.enabled) {
      control.setErrors({ ...control.errors, server: messages[0] });
      control.markAsTouched();
    } else {
      unplaced.push(...messages);
    }
  }

  return unplaced;
}

// Dates are yyyy-MM-dd, so text order is date order; a date that cannot be read is left to its own field.
export function endDateNotBeforeStart(start = 'dateFrom', end = 'dateTo'): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const from: unknown = group.get(start)?.value;
    const to: unknown = group.get(end)?.value;
    return isIsoDate(from) && isIsoDate(to) && to < from ? { dateRange: true } : null;
  };
}
