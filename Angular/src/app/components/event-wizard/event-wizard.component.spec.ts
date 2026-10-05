import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { Subject, of } from 'rxjs';
import { ApiError } from '../../core/models/api-error';
import { ToastService } from '../../core/services/toast.service';
import { EventForEdit, EventFormOptions } from '../../models/event.model';
import { EventService } from '../../services/event.service';
import { EventWizardComponent } from './event-wizard.component';

describe('EventWizardComponent', () => {
  const venueId = 'f0c1b6a2-3c4d-4e5f-8a9b-0c1d2e3f4a5b';

  const options: EventFormOptions = {
    categories: [{ publicId: 'c-music', name: 'Music' }],
    venues: [{ publicId: venueId, name: 'Zetra', city: 'Sarajevo', capacity: 1000 }],
    eventTypes: [{ publicId: 't1', name: 'Concert' }],
    organizerCompanies: [{ publicId: 'o1', name: 'Ticksi Live' }]
  };

  const event: EventForEdit = {
    publicId: 'e1',
    name: 'Hamlet',
    description: 'A play.',
    date: '2026-11-14T19:30:00',
    contact: 'box@theatre.ba',
    posterUrl: null,
    categoryId: 'c-archived',
    categoryName: 'Archived theatre',
    eventTypeId: 't1',
    locationId: venueId,
    organizerCompanyId: 'o1',
    ticketTypes: [
      { publicId: 'p1', name: 'Standard', price: 20, quantity: 50, quantityReserved: 0 },
      { publicId: 'p2', name: 'VIP', price: 45, quantity: 10, quantityReserved: 0 }
    ]
  };

  let fixture: ComponentFixture<EventWizardComponent>;
  let element: HTMLElement;
  let update: Subject<void>;
  let events: jasmine.SpyObj<EventService>;

  beforeEach(() => {
    jasmine.clock().install();
    jasmine.clock().mockDate(new Date('2026-10-05T12:00:00'));

    update = new Subject<void>();
    events = jasmine.createSpyObj<EventService>('EventService', ['getFormOptions', 'getEventForEdit', 'updateEvent', 'createEvent']);
    events.getFormOptions.and.returnValue(of(options));
    events.getEventForEdit.and.returnValue(of(event));
    events.updateEvent.and.returnValue(update);

    TestBed.configureTestingModule({
      imports: [EventWizardComponent],
      providers: [
        provideRouter([]),
        { provide: EventService, useValue: events },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'e1' }) } } },
        { provide: ToastService, useValue: jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error', 'info']) }
      ]
    });

    spyOn(TestBed.inject(Router), 'navigateByUrl').and.resolveTo(true);
    fixture = TestBed.createComponent(EventWizardComponent);
    element = fixture.nativeElement;
    fixture.detectChanges();
  });

  afterEach(() => jasmine.clock().uninstall());

  function openStep(index: number): void {
    element.querySelectorAll<HTMLButtonElement>('.track__button')[index].click();
    fixture.detectChanges();
  }

  function clickNavButton(text: string): void {
    const button = [...element.querySelectorAll<HTMLButtonElement>('.nav button')].find(b => b.textContent!.includes(text))!;
    button.click();
    fixture.detectChanges();
  }

  function currentStep(): string {
    return element.querySelector('.track__item.is-current')!.textContent!.trim();
  }

  it('shows the event\'s current category even when it is no longer offered', async () => {
    await fixture.whenStable();
    fixture.detectChanges();

    expect(element.querySelector('mat-select[formControlName="categoryId"]')!.textContent).toContain('Archived theatre');
  });

  it('locks the form while saving, so an API error lands on the row that was sent', () => {
    openStep(3);
    clickNavButton('Save changes');

    openStep(2);
    const removeButtons = element.querySelectorAll<HTMLButtonElement>('.row__remove');
    expect([...removeButtons].every(button => button.disabled)).toBeTrue();
    expect(element.querySelector<HTMLButtonElement>('.add')!.disabled).toBeTrue();
    expect(element.querySelector<HTMLInputElement>('.row input')!.disabled).toBeTrue();

    update.error(new ApiError(400, 'validation_failed', 'Invalid.', { 'ticketTypes[0].Quantity': ['Too few left.'] }));
    fixture.detectChanges();

    expect(currentStep()).toContain('Tickets');
    const firstRow = element.querySelectorAll('.row')[0];
    expect(firstRow.querySelector('input')!.value).toBe('Standard');
    expect(firstRow.querySelector('mat-error')?.textContent).toContain('Too few left.');
    expect(element.querySelector<HTMLInputElement>('.row input')!.disabled).toBeFalse();
  });

  it('checks the date again before saving, in case the event start has passed meanwhile', () => {
    openStep(3);
    jasmine.clock().mockDate(new Date('2026-11-14T20:00:00'));

    clickNavButton('Save changes');

    expect(events.updateEvent).not.toHaveBeenCalled();
    expect(currentStep()).toContain('Venue & date');
    expect(element.querySelector('mat-error')?.textContent).toContain('future');
  });

  it('checks the date again before moving on to the next step', () => {
    openStep(1);
    jasmine.clock().mockDate(new Date('2026-11-14T20:00:00'));

    clickNavButton('Next');

    expect(currentStep()).toContain('Venue & date');
    expect(element.querySelector('mat-error')?.textContent).toContain('future');
  });
});
