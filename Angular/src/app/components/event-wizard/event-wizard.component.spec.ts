import { HttpEvent, HttpEventType, HttpResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { Subject, of } from 'rxjs';
import { ApiError } from '../../core/models/api-error';
import { ToastService } from '../../core/services/toast.service';
import { EventForEdit, EventFormOptions, EventPoster } from '../../models/event.model';
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
  let posterUpload: Subject<HttpEvent<EventPoster>>;
  let events: jasmine.SpyObj<EventService>;
  let toast: jasmine.SpyObj<ToastService>;
  let router: Router;

  beforeEach(() => {
    jasmine.clock().install();
    jasmine.clock().mockDate(new Date('2026-10-05T12:00:00'));

    update = new Subject<void>();
    posterUpload = new Subject<HttpEvent<EventPoster>>();
    events = jasmine.createSpyObj<EventService>('EventService', [
      'getFormOptions',
      'getEventForEdit',
      'updateEvent',
      'createEvent',
      'uploadPoster',
      'toAssetUrl'
    ]);
    events.getFormOptions.and.returnValue(of(options));
    events.getEventForEdit.and.returnValue(of(event));
    events.updateEvent.and.returnValue(update);
    events.uploadPoster.and.callFake(() => posterUpload);
    toast = jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error', 'info']);

    TestBed.configureTestingModule({
      imports: [EventWizardComponent],
      providers: [
        provideRouter([]),
        { provide: EventService, useValue: events },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'e1' }) } } },
        { provide: ToastService, useValue: toast }
      ]
    });

    router = TestBed.inject(Router);
    spyOn(router, 'navigateByUrl').and.resolveTo(true);
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

  function dropPoster(): File {
    const poster = new File([new Uint8Array(64)], 'hamlet.png', { type: 'image/png' });
    const transfer = new DataTransfer();
    transfer.items.add(poster);
    element.querySelector('.drop')!.dispatchEvent(new DragEvent('drop', { dataTransfer: transfer, cancelable: true }));
    fixture.detectChanges();
    return poster;
  }

  function saveChanges(): void {
    openStep(4);
    clickNavButton('Save changes');
    update.next();
    update.complete();
    fixture.detectChanges();
  }

  function clickButton(text: string): void {
    [...element.querySelectorAll<HTMLButtonElement>('button')].find(b => b.textContent!.includes(text))!.click();
    fixture.detectChanges();
  }

  it('shows the event\'s current category even when it is no longer offered', async () => {
    await fixture.whenStable();
    fixture.detectChanges();

    expect(element.querySelector('mat-select[formControlName="categoryId"]')!.textContent).toContain('Archived theatre');
  });

  it('locks the form while saving, so an API error lands on the row that was sent', () => {
    openStep(4);
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
    openStep(4);
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

  it('uploads the chosen poster to the saved event with progress from the upload itself', () => {
    openStep(3);
    const poster = dropPoster();

    saveChanges();

    expect(events.uploadPoster).toHaveBeenCalledOnceWith('e1', poster);
    posterUpload.next({ type: HttpEventType.UploadProgress, loaded: 2, total: 5 });
    fixture.detectChanges();
    expect(element.querySelector('.meter__value')!.textContent).toContain('40%');
    expect(router.navigateByUrl).not.toHaveBeenCalled();

    posterUpload.next(new HttpResponse({ body: { posterUrl: '/images/events/hamlet.png' } }));

    expect(toast.success).toHaveBeenCalledWith('Changes to "Hamlet" were saved.');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/organizer/events');
  });

  it('keeps the saved event when the upload fails and retries only the upload', () => {
    openStep(3);
    dropPoster();
    saveChanges();

    posterUpload.error(new ApiError(400, 'validation_failed', 'Invalid.', { File: ['The file is not a valid image of its type.'] }));
    fixture.detectChanges();

    expect(element.querySelector('.error')!.textContent).toContain('The file is not a valid image of its type.');
    expect(router.navigateByUrl).not.toHaveBeenCalled();
    const chooseAnother = [...element.querySelectorAll<HTMLButtonElement>('app-poster-upload button')].find(b => b.textContent!.includes('Choose another'))!;
    expect(chooseAnother.disabled).toBeFalse();

    posterUpload = new Subject<HttpEvent<EventPoster>>();
    clickButton('Try again');
    posterUpload.next(new HttpResponse({ body: { posterUrl: '/images/events/hamlet.png' } }));

    expect(events.uploadPoster).toHaveBeenCalledTimes(2);
    expect(events.updateEvent).toHaveBeenCalledTimes(1);
    expect(router.navigateByUrl).toHaveBeenCalledWith('/organizer/events');
  });

  it('lets the organizer go on without the poster after a failed upload', () => {
    openStep(3);
    dropPoster();
    saveChanges();
    posterUpload.error(new ApiError(0, 'network_error', 'Cannot reach the server.'));
    fixture.detectChanges();

    clickButton('Continue without it');

    expect(toast.info).toHaveBeenCalledWith('"Hamlet" is saved without the new poster.');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/organizer/events');
  });

  it('saves without an upload when no poster was chosen', () => {
    saveChanges();

    expect(events.uploadPoster).not.toHaveBeenCalled();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/organizer/events');
  });
});
