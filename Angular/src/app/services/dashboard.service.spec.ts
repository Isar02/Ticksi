import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Dashboard, orderEventsLabel } from '../models/dashboard.model';
import { DashboardService } from './dashboard.service';

describe('DashboardService', () => {
  let service: DashboardService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(DashboardService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads the dashboard and reads the order times as UTC', () => {
    let dashboard: Dashboard | undefined;
    service.get().subscribe(result => (dashboard = result));

    http.expectOne(request => request.url.endsWith('/dashboard')).flush({
      upcomingTickets: 0,
      nextEvent: null,
      favorites: 0,
      recentOrders: [
        { orderId: 'a', eventNames: [], tickets: 1, totalAmount: 10, status: 'Paid', createdAtUtc: '2026-10-06T08:30:00' },
        { orderId: 'b', eventNames: [], tickets: 1, totalAmount: 10, status: 'Paid', createdAtUtc: '2026-10-06T08:30:00Z' }
      ],
      sales: null
    });

    expect(dashboard!.recentOrders.map(order => order.createdAtUtc)).toEqual(['2026-10-06T08:30:00Z', '2026-10-06T08:30:00Z']);
  });

  it('names an order by its first event and counts the rest', () => {
    expect(orderEventsLabel(['Jazz Night'])).toBe('Jazz Night');
    expect(orderEventsLabel(['Jazz Night', 'Winter Gala', 'Derby Day'])).toBe('Jazz Night + 2 more');
    expect(orderEventsLabel([])).toBe('Order');
  });
});
