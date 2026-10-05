import { AbstractControl, FormArray, FormControl, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { EventForEdit, EventInput, TicketTypeForEdit } from '../../models/event.model';

export const EVENT_LIMITS = { name: 200, description: 4000, contact: 200, ticketName: 100 } as const;

export const WIZARD_STEPS = ['details', 'schedule', 'tickets', 'poster'] as const;
export type WizardStep = (typeof WIZARD_STEPS)[number];

export type TicketTypeForm = FormGroup<{
  publicId: FormControl<string | null>;
  name: FormControl<string>;
  price: FormControl<number | null>;
  quantity: FormControl<number | null>;
  reserved: FormControl<number>;
}>;

export type EventWizardForm = ReturnType<typeof createEventWizardForm>;

export function createEventWizardForm(capacityOf: (locationId: string) => number | null, now: () => Date = () => new Date()) {
  const text = (maxLength: number) =>
    new FormControl('', { nonNullable: true, validators: [Validators.required, notBlank, Validators.maxLength(maxLength)] });
  const choice = () => new FormControl('', { nonNullable: true, validators: Validators.required });

  const form = new FormGroup({
    details: new FormGroup({
      name: text(EVENT_LIMITS.name),
      categoryId: choice(),
      eventTypeId: choice(),
      organizerCompanyId: choice(),
      description: text(EVENT_LIMITS.description),
      contact: text(EVENT_LIMITS.contact)
    }),
    schedule: new FormGroup({
      locationId: choice(),
      date: new FormControl('', { nonNullable: true, validators: [Validators.required, inFuture(now)] }),
      time: new FormControl('', { nonNullable: true, validators: Validators.required })
    }),
    tickets: new FormArray<TicketTypeForm>([createTicketTypeForm()], {
      validators: [Validators.required, uniqueTicketNames, withinVenueCapacity(capacityOf)]
    }),
    poster: new FormControl<File | null>(null)
  });

  const { schedule, tickets } = form.controls;
  schedule.controls.time.valueChanges.subscribe(() => schedule.controls.date.updateValueAndValidity());
  schedule.controls.locationId.valueChanges.subscribe(() => tickets.updateValueAndValidity());

  return form;
}

export function createTicketTypeForm(ticketType?: TicketTypeForEdit): TicketTypeForm {
  const reserved = ticketType?.quantityReserved ?? 0;

  return new FormGroup({
    publicId: new FormControl(ticketType?.publicId ?? null),
    name: new FormControl(ticketType?.name ?? '', {
      nonNullable: true,
      validators: [Validators.required, notBlank, Validators.maxLength(EVENT_LIMITS.ticketName)]
    }),
    price: new FormControl<number | null>(ticketType?.price ?? null, {
      validators: [Validators.required, Validators.min(0), atMostTwoDecimals]
    }),
    quantity: new FormControl<number | null>(ticketType?.quantity ?? null, {
      validators: [Validators.required, wholeNumber, Validators.min(Math.max(1, reserved))]
    }),
    reserved: new FormControl(reserved, { nonNullable: true })
  });
}

export function fillFromEvent(form: EventWizardForm, event: EventForEdit): void {
  const [date, time] = event.date.split('T');

  form.controls.details.setValue({
    name: event.name,
    categoryId: event.categoryId,
    eventTypeId: event.eventTypeId,
    organizerCompanyId: event.organizerCompanyId,
    description: event.description,
    contact: event.contact
  });
  form.controls.schedule.setValue({ locationId: event.locationId, date, time: time.slice(0, 5) });

  form.controls.tickets.clear({ emitEvent: false });
  event.ticketTypes.forEach(ticketType => form.controls.tickets.push(createTicketTypeForm(ticketType), { emitEvent: false }));
  form.controls.tickets.updateValueAndValidity();
}

export function toEventInput(form: EventWizardForm): EventInput {
  const { details, schedule, tickets } = form.getRawValue();

  return {
    name: details.name.trim(),
    description: details.description.trim(),
    contact: details.contact.trim(),
    categoryId: details.categoryId,
    eventTypeId: details.eventTypeId,
    organizerCompanyId: details.organizerCompanyId,
    locationId: schedule.locationId,
    date: `${schedule.date}T${schedule.time}:00`,
    ticketTypes: tickets.map(ticket => ({
      publicId: ticket.publicId,
      name: ticket.name.trim(),
      price: ticket.price ?? 0,
      quantity: ticket.quantity ?? 0
    }))
  };
}

// The start can pass while the form stays open; an error from the API is kept until the date is changed.
export function recheckDate(form: EventWizardForm): void {
  const date = form.controls.schedule.controls.date;
  if (!date.hasError('server')) date.updateValueAndValidity();
}

export function ticketTotal(tickets: FormArray<TicketTypeForm>): number {
  return tickets.getRawValue().reduce((total, ticket) => total + (ticket.quantity ?? 0), 0);
}

export interface ServerErrorsResult {
  firstStep: WizardStep | null;
  unplaced: string[];
}

// Puts each API field error on its control; returns the earliest step holding one and the messages that fit nowhere.
export function applyServerErrors(form: EventWizardForm, fieldErrors: Readonly<Record<string, string[]>>): ServerErrorsResult {
  let firstStep: WizardStep | null = null;
  const unplaced: string[] = [];

  for (const [field, messages] of Object.entries(fieldErrors)) {
    const target = controlForField(form, field);
    if (!target) {
      unplaced.push(...messages);
      continue;
    }

    target.control.setErrors({ ...target.control.errors, server: messages[0] });
    target.control.markAsTouched();
    if (firstStep === null || WIZARD_STEPS.indexOf(target.step) < WIZARD_STEPS.indexOf(firstStep)) {
      firstStep = target.step;
    }
  }

  return { firstStep, unplaced };
}

export function errorText(control: AbstractControl, label: string): string {
  const errors = control.errors ?? {};

  if (errors['server']) return errors['server'];
  if (errors['required']) return `${label} is required.`;
  if (errors['maxlength']) return `${label} can have at most ${errors['maxlength'].requiredLength} characters.`;
  if (errors['past']) return 'Pick a date and time in the future.';
  if (errors['wholeNumber']) return `${label} must be a whole number.`;
  if (errors['decimals']) return `${label} can have at most two decimals.`;
  if (errors['min']) return `${label} must be at least ${errors['min'].min}.`;
  return '';
}

function controlForField(form: EventWizardForm, field: string): { step: WizardStep; control: AbstractControl } | null {
  const key = field.replace(/^\$\./, '').toLowerCase();
  const { details, schedule, tickets } = form.controls;

  const detailsControl = Object.entries(details.controls).find(([name]) => name.toLowerCase() === key)?.[1];
  if (detailsControl) return { step: 'details', control: detailsControl };

  if (key === 'locationid') return { step: 'schedule', control: schedule.controls.locationId };
  if (key === 'date') return { step: 'schedule', control: schedule.controls.date };
  if (key === 'tickettypes') return { step: 'tickets', control: tickets };

  const row = /^tickettypes\[(\d+)\](?:\.(\w+))?$/.exec(key);
  const ticket = row ? tickets.at(Number(row[1])) : undefined;
  if (!row || !ticket) return null;

  const property = row[2];
  const control = property === 'name' || property === 'price' || property === 'quantity' ? ticket.controls[property] : ticket;
  return { step: 'tickets', control };
}

function notBlank(control: AbstractControl<string>): ValidationErrors | null {
  return control.value && !control.value.trim() ? { required: true } : null;
}

function wholeNumber(control: AbstractControl<number | null>): ValidationErrors | null {
  return control.value !== null && !Number.isInteger(control.value) ? { wholeNumber: true } : null;
}

function atMostTwoDecimals(control: AbstractControl<number | null>): ValidationErrors | null {
  return control.value !== null && Number(control.value.toFixed(2)) !== control.value ? { decimals: true } : null;
}

// Until a time is picked, any moment left in the chosen day still counts as the future.
function inFuture(now: () => Date): ValidatorFn {
  return (control: AbstractControl<string>) => {
    const time = control.parent?.get('time')?.value || '23:59';
    return control.value && new Date(`${control.value}T${time}`) <= now() ? { past: true } : null;
  };
}

function uniqueTicketNames(control: AbstractControl): ValidationErrors | null {
  const names = (control as FormArray<TicketTypeForm>).getRawValue().map(ticket => ticket.name.trim().toLowerCase());
  const filled = names.filter(Boolean);
  return new Set(filled).size !== filled.length ? { duplicateNames: true } : null;
}

function withinVenueCapacity(capacityOf: (locationId: string) => number | null): ValidatorFn {
  return control => {
    const locationId = control.parent?.get('schedule.locationId')?.value as string | undefined;
    const capacity = locationId ? capacityOf(locationId) : null;
    const total = ticketTotal(control as FormArray<TicketTypeForm>);
    return capacity !== null && total > capacity ? { capacity: { capacity, total } } : null;
  };
}
