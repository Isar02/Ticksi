import { EventForEdit } from '../../models/event.model';
import { applyServerErrors, createEventWizardForm, createTicketTypeForm, fillFromEvent, toEventInput } from './event-wizard-form';

describe('event wizard form', () => {
  const venueId = 'f0c1b6a2-3c4d-4e5f-8a9b-0c1d2e3f4a5b';
  const now = new Date('2026-10-05T12:00:00');
  const createForm = (capacity = 100) => createEventWizardForm(id => (id === venueId ? capacity : null), () => now);

  const editedEvent: EventForEdit = {
    publicId: 'e1',
    name: 'Hamlet',
    description: 'A play.',
    date: '2026-11-14T19:30:00',
    contact: 'box@theatre.ba',
    posterUrl: null,
    categoryId: 'c1',
    categoryName: 'Theatre',
    eventTypeId: 't1',
    locationId: venueId,
    organizerCompanyId: 'o1',
    ticketTypes: [
      { publicId: 'p1', name: 'Standard', price: 20, quantity: 50, quantityReserved: 12 },
      { publicId: 'p2', name: 'VIP', price: 45.5, quantity: 10, quantityReserved: 0 }
    ]
  };

  it('refuses a date and time that has already passed', () => {
    const { schedule } = createForm().controls;

    schedule.patchValue({ date: '2026-10-05', time: '11:00' });
    expect(schedule.controls.date.hasError('past')).toBeTrue();

    schedule.patchValue({ time: '13:00' });
    expect(schedule.controls.date.hasError('past')).toBeFalse();
  });

  it('refuses ticket names that repeat, ignoring case and spaces', () => {
    const { tickets } = createForm().controls;
    tickets.at(0).patchValue({ name: 'VIP' });
    tickets.push(createTicketTypeForm());

    tickets.at(1).patchValue({ name: ' vip ' });
    expect(tickets.hasError('duplicateNames')).toBeTrue();

    tickets.at(1).patchValue({ name: 'Standard' });
    expect(tickets.hasError('duplicateNames')).toBeFalse();
  });

  it('checks prices for sign and at most two decimals', () => {
    const price = createForm().controls.tickets.at(0).controls.price;

    price.setValue(-1);
    expect(price.hasError('min')).toBeTrue();
    price.setValue(9.999);
    expect(price.hasError('decimals')).toBeTrue();
    price.setValue(0);
    expect(price.valid).toBeTrue();
    price.setValue(12.3);
    expect(price.valid).toBeTrue();
  });

  it('keeps the ticket total within the venue capacity, also when the venue changes', () => {
    const form = createForm(100);
    form.controls.tickets.at(0).patchValue({ name: 'Standard', price: 10, quantity: 120 });
    expect(form.controls.tickets.hasError('capacity')).toBeFalse();

    form.controls.schedule.patchValue({ locationId: venueId });
    expect(form.controls.tickets.hasError('capacity')).toBeTrue();

    form.controls.tickets.at(0).patchValue({ quantity: 100 });
    expect(form.controls.tickets.valid).toBeTrue();
  });

  it('loads an event for editing without letting a quantity drop below the reserved tickets', () => {
    const form = createForm();
    fillFromEvent(form, editedEvent);

    expect(form.controls.schedule.getRawValue()).toEqual({ locationId: venueId, date: '2026-11-14', time: '19:30' });
    expect(form.controls.tickets.length).toBe(2);
    expect(form.valid).toBeTrue();

    const standard = form.controls.tickets.at(0).controls.quantity;
    standard.setValue(11);
    expect(standard.hasError('min')).toBeTrue();
    standard.setValue(12);
    expect(standard.valid).toBeTrue();
  });

  it('sends trimmed text, a local date and time and the kept ticket ids', () => {
    const form = createForm();
    fillFromEvent(form, editedEvent);
    form.controls.details.patchValue({ name: '  Hamlet  ' });
    form.controls.tickets.push(createTicketTypeForm());
    form.controls.tickets.at(2).patchValue({ name: ' Student ', price: 5, quantity: 5 });

    const input = toEventInput(form);

    expect(input.name).toBe('Hamlet');
    expect(input.date).toBe('2026-11-14T19:30:00');
    expect(input.ticketTypes).toEqual([
      { publicId: 'p1', name: 'Standard', price: 20, quantity: 50 },
      { publicId: 'p2', name: 'VIP', price: 45.5, quantity: 10 },
      { publicId: null, name: 'Student', price: 5, quantity: 5 }
    ]);
  });

  it('puts API errors on their fields and points to the earliest step holding one', () => {
    const form = createForm();
    fillFromEvent(form, editedEvent);

    const result = applyServerErrors(form, {
      'ticketTypes[0].Quantity': ['12 tickets of this type are already sold or reserved.'],
      locationId: ['The selected venue does not exist.'],
      'ticketTypes[1].PublicId': ['This ticket type does not belong to the event.'],
      '$.somethingElse': ['The value is invalid.']
    });

    expect(result.firstStep).toBe('schedule');
    expect(result.unplaced).toEqual(['The value is invalid.']);
    expect(form.controls.schedule.controls.locationId.getError('server')).toBe('The selected venue does not exist.');
    expect(form.controls.tickets.at(0).controls.quantity.getError('server')).toContain('already sold');
    expect(form.controls.tickets.at(1).getError('server')).toContain('does not belong');

    form.controls.schedule.controls.locationId.setValue(venueId);
    expect(form.controls.schedule.controls.locationId.hasError('server')).toBeFalse();
  });

  it('maps a list-wide ticket error and a details field to their steps', () => {
    const form = createForm();

    const result = applyServerErrors(form, {
      ticketTypes: ['The venue holds 100 people, so the ticket quantities cannot add up to more.'],
      categoryId: ['The selected category does not exist.']
    });

    expect(result.firstStep).toBe('details');
    expect(form.controls.tickets.getError('server')).toContain('venue holds');
    expect(form.controls.details.controls.categoryId.getError('server')).toContain('category');
  });
});
