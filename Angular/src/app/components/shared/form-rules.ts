import { AbstractControl, FormGroup, ValidationErrors, ValidatorFn } from '@angular/forms';

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
