import { DEFAULT_CURRENCY_CODE, LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeBs from '@angular/common/locales/bs';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { DashboardSales } from '../../../models/dashboard.model';
import { DashboardSalesComponent } from './dashboard-sales.component';

describe('DashboardSalesComponent', () => {
  let fixture: ComponentFixture<DashboardSalesComponent>;

  const organizer: DashboardSales = {
    allEvents: false,
    upcomingEvents: 3,
    ticketsSold: 12,
    revenue: 1234.5,
    activeUsers: null,
    topEvents: [
      { eventId: 'jazz-id', name: 'Jazz Night', date: '2026-11-08T20:00:00', ticketsSold: 8, revenue: 200 },
      { eventId: 'gala-id', name: 'Winter Gala', date: '2027-01-15T19:00:00', ticketsSold: 4, revenue: 1034.5 }
    ]
  };

  beforeAll(() => registerLocaleData(localeBs));

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'bs' },
        { provide: DEFAULT_CURRENCY_CODE, useValue: 'BAM' }
      ]
    });
    fixture = TestBed.createComponent(DashboardSalesComponent);
  });

  function show(sales: DashboardSales): HTMLElement {
    fixture.componentRef.setInput('sales', sales);
    fixture.detectChanges();
    return fixture.nativeElement;
  }

  function values(page: HTMLElement): string[] {
    return Array.from(page.querySelectorAll('.tile__value')).map(value => value.textContent!.replace(/\s+/g, ' ').trim());
  }

  it('shows the organizer the figures of their events and ranks the best sellers', () => {
    const page = show(organizer);

    expect(page.querySelector('.sales__scope')!.textContent).toBe('Your events');
    expect(values(page)).toEqual(['1.234,50 KM', '12', '3']);
    expect(page.querySelector('a.tile[href="/organizer/events?period=upcoming"]')).not.toBeNull();
    expect(page.querySelector('a.tile[href="/admin/users"]')).toBeNull();

    const items = Array.from(page.querySelectorAll('.best__item'));
    expect(items.map(item => item.querySelector('.best__name')!.textContent)).toEqual(['Jazz Night', 'Winter Gala']);
    expect(items.map(item => (item.querySelector('.best__fill') as HTMLElement).style.width)).toEqual(['100%', '50%']);
    expect(items[0].querySelector('.best__name')!.getAttribute('href')).toBe('/event/jazz-id');
  });

  it('shows the administrator every event and the active users', () => {
    const page = show({ ...organizer, allEvents: true, activeUsers: 42 });

    expect(page.querySelector('.sales__scope')!.textContent).toBe('All events');
    expect(values(page)).toEqual(['1.234,50 KM', '12', '3', '42']);
    expect(page.querySelector('a.tile[href="/admin/users"]')).not.toBeNull();
  });

  it('says when nothing has been sold yet', () => {
    const page = show({ ...organizer, ticketsSold: 0, revenue: 0, topEvents: [] });

    expect(page.querySelector('.best__empty')!.textContent).toContain('No paid sales yet.');
  });
});
