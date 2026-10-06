import { LOCALE_ID, signal } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeBs from '@angular/common/locales/bs';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { ApiError } from '../../core/models/api-error';
import { Dashboard } from '../../models/dashboard.model';
import { AuthService } from '../../services/auth.service';
import { DashboardService } from '../../services/dashboard.service';
import { DashboardComponent, greetingFor } from './dashboard.component';

describe('DashboardComponent', () => {
  let fixture: ComponentFixture<DashboardComponent>;
  let dashboard$: Subject<Dashboard>;
  let loads: number;
  const sessionId = signal<string | null>('buyer-session');

  const personal: Dashboard = {
    upcomingTickets: 2,
    nextEvent: {
      eventId: 'jazz-id',
      name: 'Jazz Night',
      date: '2026-11-08T20:00:00',
      venueName: 'Zetra',
      venueCity: 'Sarajevo',
      tickets: 2
    },
    favorites: 1,
    recentOrders: [],
    sales: null
  };

  beforeAll(() => registerLocaleData(localeBs));

  beforeEach(() => {
    loads = 0;
    sessionId.set('buyer-session');
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'bs' },
        { provide: AuthService, useValue: { sessionId: () => sessionId(), currentUser: () => ({ firstName: 'Emir' }) } },
        {
          provide: DashboardService,
          useValue: {
            get: () => {
              loads++;
              dashboard$ = new Subject<Dashboard>();
              return dashboard$;
            }
          }
        }
      ]
    });

    fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();
  });

  function page(): HTMLElement {
    return fixture.nativeElement;
  }

  function show(dashboard: Dashboard): void {
    dashboard$.next(dashboard);
    fixture.detectChanges();
  }

  it('shows skeletons, then the personal overview without a sales section for buyers', () => {
    expect(page().querySelector('[aria-busy="true"]')).not.toBeNull();
    expect(page().querySelector('.head__title')!.textContent).toContain('Emir');

    show(personal);

    expect(page().querySelector('app-dashboard-next-event')!.textContent).toContain('Jazz Night');
    expect(Array.from(page().querySelectorAll('.tile__value')).map(v => v.textContent!.trim())).toEqual(['2', '1']);
    expect(page().querySelector('a.tile[href="/tickets"]')).not.toBeNull();
    expect(page().querySelector('a.tile[href="/favorites"]')).not.toBeNull();
    expect(page().querySelector('app-dashboard-orders')).not.toBeNull();
    expect(page().querySelector('app-dashboard-sales')).toBeNull();
  });

  it('adds the sales section for organizers and administrators', () => {
    show({
      ...personal,
      sales: { allEvents: false, upcomingEvents: 3, ticketsSold: 7, revenue: 200, activeUsers: null, topEvents: [] }
    });

    expect(page().querySelector('app-dashboard-sales')!.textContent).toContain('Your events');
  });

  it('shows a failure with Try again, which loads the dashboard again', () => {
    dashboard$.error(new ApiError(0, 'network_error', 'Cannot reach the server.'));
    fixture.detectChanges();

    expect(page().querySelector('.state--error')!.textContent).toContain('Cannot reach the server.');

    (page().querySelector('.state--error button') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(loads).toBe(2);
    expect(page().querySelector('[aria-busy="true"]')).not.toBeNull();
  });

  it('loads again for a new sign-in and sends a sign-out from another tab to the login page', () => {
    show(personal);
    const router = TestBed.inject(Router);
    const navigate = spyOn(router, 'navigateByUrl').and.resolveTo(true);

    sessionId.set('admin-session');
    fixture.detectChanges();
    expect(loads).toBe(2);
    expect(page().querySelector('app-dashboard-next-event')).toBeNull();

    sessionId.set(null);
    fixture.detectChanges();
    expect(navigate).toHaveBeenCalledTimes(1);
    expect(router.serializeUrl(navigate.calls.mostRecent().args[0] as UrlTree)).toBe('/auth/login?returnUrl=%2Fdashboard');
  });

  it('greets by the time of day', () => {
    expect(greetingFor(new Date(2026, 9, 6, 8))).toBe('Good morning');
    expect(greetingFor(new Date(2026, 9, 6, 14))).toBe('Good afternoon');
    expect(greetingFor(new Date(2026, 9, 6, 20))).toBe('Good evening');
    expect(greetingFor(new Date(2026, 9, 6, 2))).toBe('Good evening');
  });
});
