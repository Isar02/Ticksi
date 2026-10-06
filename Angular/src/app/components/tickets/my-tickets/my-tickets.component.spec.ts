import { LOCALE_ID, signal } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeBs from '@angular/common/locales/bs';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { ApiError } from '../../../core/models/api-error';
import { Ticket } from '../../../models/ticket.model';
import { AuthService } from '../../../services/auth.service';
import { TicketService } from '../../../services/ticket.service';
import { MyTicketsComponent } from './my-tickets.component';

describe('MyTicketsComponent', () => {
  let fixture: ComponentFixture<MyTicketsComponent>;
  let tickets$: Subject<Ticket[]>;
  let loads: number;
  const session = signal<{ id: string; accessToken: string } | null>(null);
  let qr$: Subject<Blob>;

  function ticket(code: string, eventId: string, eventName: string, eventDate: string): Ticket {
    return {
      publicId: code,
      code,
      status: 'Valid',
      ticketTypeName: 'Standard',
      eventId,
      eventName,
      eventDate,
      venueName: 'Zetra',
      venueCity: 'Sarajevo',
      orderId: 'f668842e-171b-4322-a41a-35d01bb9b9b5'
    };
  }

  beforeAll(() => registerLocaleData(localeBs));

  beforeEach(() => {
    loads = 0;
    session.set({ id: 'buyer-a-session', accessToken: 'token-a' });
    qr$ = new Subject<Blob>();
    jasmine.clock().install();
    jasmine.clock().mockDate(new Date('2026-10-06T12:00:00'));
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'bs' },
        { provide: AuthService, useValue: { sessionId: () => session()?.id ?? null } },
        {
          provide: TicketService,
          useValue: {
            getMine: () => {
              loads++;
              tickets$ = new Subject<Ticket[]>();
              return tickets$;
            },
            getQrCode: () => qr$
          }
        }
      ]
    });

    fixture = TestBed.createComponent(MyTicketsComponent);
    fixture.detectChanges();
  });

  afterEach(() => jasmine.clock().uninstall());

  function page(): HTMLElement {
    return fixture.nativeElement;
  }

  function texts(selector: string): string[] {
    return Array.from(page().querySelectorAll(selector)).map(element => element.textContent!.replace(/\s+/g, ' ').trim());
  }

  it('shows the tickets by event, upcoming first and past events apart', () => {
    expect(page().querySelectorAll('.ticket--skeleton').length).toBe(3);

    tickets$.next([
      ticket('OLD1', 'old', 'Spring Jazz', '2026-04-10T20:00:00'),
      ticket('ROCK1', 'rock', 'Sarajevo Rock Night', '2026-12-09T20:00:00'),
      ticket('ROCK2', 'rock', 'Sarajevo Rock Night', '2026-12-09T20:00:00')
    ]);
    fixture.detectChanges();

    expect(texts('.head__count')).toEqual(['3']);
    expect(texts('.section')).toEqual(['Upcoming', 'Past events']);
    expect(texts('.event__name')).toEqual(['Sarajevo Rock Night', 'Spring Jazz']);
    expect(texts('.event__meta')[0]).toBe('9. 12. 2026. 20:00 · Zetra, Sarajevo');
    expect(texts('.event__count')).toEqual(['2 tickets', '1 ticket']);
    expect(page().querySelectorAll('.event--past app-ticket-card.ticket--past').length).toBe(1);
    expect(page().querySelector('.event__name a')!.getAttribute('href')).toBe('/event/rock');
  });

  it('explains how to get tickets when there are none', () => {
    tickets$.next([]);
    fixture.detectChanges();

    expect(texts('.state__title')).toEqual(['No tickets yet']);
    expect(page().querySelector('.state a')!.getAttribute('href')).toBe('/events');
    expect(page().querySelector('.section')).toBeNull();
  });

  it('offers another try after a failure', () => {
    tickets$.error(new ApiError(0, 'network', 'Cannot reach the server.'));
    fixture.detectChanges();

    expect(texts('.state--error .state__text')).toEqual(['Cannot reach the server.']);
    page().querySelector<HTMLButtonElement>('.state--error button')!.click();
    fixture.detectChanges();
    tickets$.next([ticket('ROCK1', 'rock', 'Sarajevo Rock Night', '2026-12-09T20:00:00')]);
    fixture.detectChanges();

    expect(loads).toBe(2);
    expect(texts('.section')).toEqual(['Upcoming']);
  });

  it('clears the previous buyer tickets and releases their QR images before loading the next buyer', () => {
    tickets$.next([ticket('BUYERACODE01', 'a', 'Buyer A event', '2026-12-09T20:00:00')]);
    fixture.detectChanges();
    qr$.next(new Blob(['png'], { type: 'image/png' }));
    fixture.detectChanges();
    const previousUrl = page().querySelector<HTMLImageElement>('.qr__image')!.src;
    spyOn(URL, 'revokeObjectURL').and.callThrough();

    session.set({ id: 'buyer-b-session', accessToken: 'token-b' });
    fixture.detectChanges();

    expect(loads).toBe(2);
    expect(page().querySelector('app-ticket-card')).toBeNull();
    expect(page().querySelector('.qr__image')).toBeNull();
    expect(URL.revokeObjectURL).toHaveBeenCalledWith(previousUrl);
    tickets$.next([ticket('BUYERBCODE01', 'b', 'Buyer B event', '2026-12-10T20:00:00')]);
    fixture.detectChanges();
    expect(texts('.event__name')).toEqual(['Buyer B event']);
  });

  it('clears tickets, cancels pending QR requests and redirects to login when signed out in another tab', () => {
    const router = TestBed.inject(Router);
    const navigate = spyOn(router, 'navigateByUrl').and.resolveTo(true);
    tickets$.next([ticket('BUYERACODE01', 'a', 'Buyer A event', '2026-12-09T20:00:00')]);
    fixture.detectChanges();
    expect(qr$.observed).toBeTrue();

    session.set(null);
    fixture.detectChanges();

    expect(page().querySelector('app-ticket-card')).toBeNull();
    expect(qr$.observed).toBeFalse();
    expect(loads).toBe(1);
    expect(navigate).toHaveBeenCalledOnceWith(router.createUrlTree(['/auth/login'], { queryParams: { returnUrl: '/tickets' } }));
  });

  it('leaves the navigation to the sign-out that started it in this tab', () => {
    const router = TestBed.inject(Router);
    const navigate = spyOn(router, 'navigateByUrl').and.resolveTo(true);
    spyOn(router, 'getCurrentNavigation').and.returnValue({ id: 1 } as ReturnType<Router['getCurrentNavigation']>);
    tickets$.next([ticket('BUYERACODE01', 'a', 'Buyer A event', '2026-12-09T20:00:00')]);
    fixture.detectChanges();

    session.set(null);
    fixture.detectChanges();

    expect(page().querySelector('app-ticket-card')).toBeNull();
    expect(navigate).not.toHaveBeenCalled();
  });

  it('cancels an outstanding list request and ignores its late response after the session changes', () => {
    const previousRequest = tickets$;
    session.set({ id: 'buyer-b-session', accessToken: 'token-b' });
    fixture.detectChanges();

    expect(previousRequest.observed).toBeFalse();
    previousRequest.next([ticket('BUYERACODE01', 'a', 'Buyer A event', '2026-12-09T20:00:00')]);
    fixture.detectChanges();
    expect(page().querySelector('app-ticket-card')).toBeNull();
    tickets$.next([ticket('BUYERBCODE01', 'b', 'Buyer B event', '2026-12-10T20:00:00')]);
    fixture.detectChanges();
    expect(texts('.event__name')).toEqual(['Buyer B event']);
  });

  it('keeps the current tickets when only the access token rotates', () => {
    tickets$.next([ticket('BUYERACODE01', 'a', 'Buyer A event', '2026-12-09T20:00:00')]);
    fixture.detectChanges();

    session.set({ id: 'buyer-a-session', accessToken: 'rotated-token-a' });
    fixture.detectChanges();

    expect(loads).toBe(1);
    expect(texts('.event__name')).toEqual(['Buyer A event']);
  });
});
