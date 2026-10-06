import { DEFAULT_CURRENCY_CODE, LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeBs from '@angular/common/locales/bs';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { Subject, of } from 'rxjs';
import { ApiError } from '../../../core/models/api-error';
import { Order } from '../../../models/order.model';
import { OrderService } from '../../../services/order.service';
import { OrderDetailsComponent } from './order-details.component';

describe('OrderDetailsComponent', () => {
  let fixture: ComponentFixture<OrderDetailsComponent>;
  let order$: Subject<Order>;
  let requested: string[];

  const order: Order = {
    publicId: '590d40e2-f31a-483b-9894-5118861eda63',
    status: 'Pending',
    totalAmount: 157.5,
    createdAtUtc: '2026-10-06T09:33:12Z',
    items: [
      { eventId: 'rock', eventName: 'Sarajevo Rock Night', eventDate: '2026-12-09T20:00:00', ticketTypeName: 'Standard', quantity: 2, unitPrice: 35 },
      { eventId: 'rock', eventName: 'Sarajevo Rock Night', eventDate: '2026-12-09T20:00:00', ticketTypeName: 'VIP', quantity: 1, unitPrice: 87.5 }
    ]
  };

  beforeAll(() => registerLocaleData(localeBs));

  beforeEach(() => {
    requested = [];
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'bs' },
        { provide: DEFAULT_CURRENCY_CODE, useValue: 'BAM' },
        { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({ id: order.publicId })) } },
        {
          provide: OrderService,
          useValue: {
            getOrder: (id: string) => {
              requested.push(id);
              order$ = new Subject<Order>();
              return order$;
            }
          }
        }
      ]
    });

    fixture = TestBed.createComponent(OrderDetailsComponent);
    fixture.detectChanges();
  });

  function page(): HTMLElement {
    return fixture.nativeElement;
  }

  function text(selector: string): string {
    return page().querySelector(selector)!.textContent!.replace(/\s+/g, ' ').trim();
  }

  it('shows the pending order with its lines and total', () => {
    expect(page().querySelector('.slip--skeleton')).not.toBeNull();

    order$.next(order);
    fixture.detectChanges();

    expect(requested).toEqual([order.publicId]);
    expect(text('.head__number')).toBe('#590D40E2');
    expect(text('.slip__status')).toContain('Awaiting payment');
    expect(Array.from(page().querySelectorAll('.line__meta')).map(line => line.textContent!.trim())).toEqual([
      '9. 12. 2026. 20:00 · Standard',
      '9. 12. 2026. 20:00 · VIP'
    ]);
    expect(text('.line__count')).toContain('2 × 35,00');
    expect(text('.slip__total')).toContain('3 tickets');
    expect(text('.slip__total')).toContain('157,50');
  });

  it('shows a paid order without the reservation note', () => {
    order$.next({ ...order, status: 'Paid' });
    fixture.detectChanges();

    expect(page().querySelector('.slip__status--paid')).not.toBeNull();
    expect(text('.slip__status strong')).toBe('Paid');
    expect(page().querySelector('.slip__status span')).toBeNull();
  });

  it('shows a not-found state for an unknown or foreign order', () => {
    order$.error(new ApiError(404, 'not_found', 'Order not found.'));
    fixture.detectChanges();

    expect(text('.state__title')).toBe('Order not found');
    expect(page().querySelector('a[href="/events"]')).not.toBeNull();
  });

  it('offers another try after a failure', () => {
    order$.error(new ApiError(0, 'network', 'Cannot reach the server.'));
    fixture.detectChanges();

    expect(text('.state--error .state__text')).toBe('Cannot reach the server.');
    page().querySelector<HTMLButtonElement>('.state--error button')!.click();
    fixture.detectChanges();
    order$.next(order);
    fixture.detectChanges();

    expect(requested.length).toBe(2);
    expect(page().querySelector('.slip__lines')).not.toBeNull();
  });
});
