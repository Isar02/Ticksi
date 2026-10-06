import { DEFAULT_CURRENCY_CODE, LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeBs from '@angular/common/locales/bs';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { DashboardOrder } from '../../../models/dashboard.model';
import { DashboardOrdersComponent } from './dashboard-orders.component';

describe('DashboardOrdersComponent', () => {
  let fixture: ComponentFixture<DashboardOrdersComponent>;

  beforeAll(() => registerLocaleData(localeBs));

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'bs' },
        { provide: DEFAULT_CURRENCY_CODE, useValue: 'BAM' }
      ]
    });
    fixture = TestBed.createComponent(DashboardOrdersComponent);
  });

  function show(orders: DashboardOrder[]): HTMLElement {
    fixture.componentRef.setInput('orders', orders);
    fixture.detectChanges();
    return fixture.nativeElement;
  }

  it('lists each order with its events, time, tickets, total and status, linked to the order', () => {
    const page = show([
      {
        orderId: 'order-1',
        eventNames: ['Jazz Night', 'Winter Gala'],
        tickets: 3,
        totalAmount: 75,
        status: 'Pending',
        createdAtUtc: '2026-10-06T08:30:00Z'
      },
      { orderId: 'order-2', eventNames: ['Derby Day'], tickets: 1, totalAmount: 25, status: 'Paid', createdAtUtc: '2026-10-05T18:00:00Z' }
    ]);

    const rows = Array.from(page.querySelectorAll('.order'));
    expect(rows.map(row => row.getAttribute('href'))).toEqual(['/orders/order-1', '/orders/order-2']);
    expect(rows[0].querySelector('.order__events')!.textContent).toBe('Jazz Night + 1 more');
    expect(rows[0].querySelector('.order__events')!.getAttribute('title')).toBe('Jazz Night, Winter Gala');
    expect(rows[0].querySelector('.order__meta')!.textContent).toContain('3 tickets');
    expect(rows[0].querySelector('.order__total')!.textContent!.replace(/\s+/g, ' ')).toContain('75,00 KM');
    expect(rows[1].querySelector('.order__status--paid')!.textContent).toBe('Paid');
    expect(rows[1].querySelector('.order__meta')!.textContent).toContain('1 ticket');
  });

  it('says when there are no orders yet', () => {
    expect(show([]).querySelector('.empty')!.textContent).toContain('No orders yet.');
  });
});
