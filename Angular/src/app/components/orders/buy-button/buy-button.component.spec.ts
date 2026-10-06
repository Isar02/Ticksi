import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Event } from '../../../models/event.model';
import { BuyTicketsDialogService } from '../buy-tickets-dialog/buy-tickets-dialog.service';
import { BuyButtonComponent } from './buy-button.component';

describe('BuyButtonComponent', () => {
  let fixture: ComponentFixture<BuyButtonComponent>;
  let opened: Event[];

  const event: Event = {
    publicId: 'rock',
    name: 'Sarajevo Rock Night',
    description: '',
    date: new Date(Date.now() + 86_400_000).toISOString(),
    contact: '',
    posterUrl: null,
    lowestPrice: 35,
    availableTickets: 120,
    eventCategoryName: 'Music',
    eventCategoryPublicId: '',
    locationName: 'Zetra',
    eventTypeName: '',
    organizerCompanyName: ''
  };

  beforeEach(() => {
    opened = [];
    TestBed.configureTestingModule({
      providers: [{ provide: BuyTicketsDialogService, useValue: { open: (item: Event) => opened.push(item) } }]
    });
    fixture = TestBed.createComponent(BuyButtonComponent);
  });

  function render(changes: Partial<Event>): HTMLButtonElement | null {
    fixture.componentRef.setInput('event', { ...event, ...changes });
    fixture.detectChanges();
    return fixture.nativeElement.querySelector('button');
  }

  it('opens the purchase for an upcoming event with tickets left', () => {
    const button = render({});

    expect(button!.getAttribute('aria-label')).toBe('Buy tickets: Sarajevo Rock Night');
    button!.click();
    expect(opened.map(item => item.publicId)).toEqual(['rock']);
  });

  it('hides itself when nothing can be bought', () => {
    expect(render({ availableTickets: 0 })).toBeNull();
    expect(render({ lowestPrice: null, availableTickets: 0 })).toBeNull();
    expect(render({ date: new Date(Date.now() - 60_000).toISOString() })).toBeNull();
  });

  it('hides the button when the event starts on an open page', fakeAsync(() => {
    const start = Date.now() + 60_000;
    expect(render({ date: new Date(start).toISOString() })).not.toBeNull();

    tick(60_000);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('button')).toBeNull();
    fixture.destroy();
  }));

  it('checks the current time before opening even if the scheduled update has not run', () => {
    const start = Date.now() + 60_000;
    const button = render({ date: new Date(start).toISOString() });
    spyOn(Date, 'now').and.returnValue(start);

    button!.click();
    fixture.detectChanges();

    expect(opened).toEqual([]);
    expect(fixture.nativeElement.querySelector('button')).toBeNull();
  });

  it('reschedules the expiry when the event changes', fakeAsync(() => {
    const now = Date.now();
    render({ date: new Date(now + 60_000).toISOString() });
    render({ date: new Date(now + 120_000).toISOString() });

    tick(60_000);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button')).not.toBeNull();
    tick(60_000);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button')).toBeNull();
    fixture.destroy();
  }));
});
