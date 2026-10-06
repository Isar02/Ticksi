import { DEFAULT_CURRENCY_CODE, LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeBs from '@angular/common/locales/bs';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormGroupDirective } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Subject } from 'rxjs';
import { ApiError } from '../../../core/models/api-error';
import { ToastService } from '../../../core/services/toast.service';
import { Order, OrderLineInput, TicketTypeOffer } from '../../../models/order.model';
import { EventService } from '../../../services/event.service';
import { OrderService } from '../../../services/order.service';
import { BuyTicketsDialogComponent } from './buy-tickets-dialog.component';

describe('BuyTicketsDialogComponent', () => {
  let fixture: ComponentFixture<BuyTicketsDialogComponent>;
  let offers$: Subject<TicketTypeOffer[]>;
  let order$: Subject<Order>;
  let orders: OrderLineInput[][];
  let loads: number;
  let toasts: string[];
  let closedWith: (string | undefined)[];
  let dialogRef: { disableClose: boolean; close: (result?: string) => void };

  const offers: TicketTypeOffer[] = [
    { publicId: 'standard', name: 'Standard', price: 35, available: 500 },
    { publicId: 'vip', name: 'VIP', price: 87.5, available: 3 },
    { publicId: 'balcony', name: 'Balcony', price: 20, available: 0 }
  ];

  beforeAll(() => registerLocaleData(localeBs));

  beforeEach(() => {
    orders = [];
    loads = 0;
    toasts = [];
    closedWith = [];
    dialogRef = { disableClose: false, close: result => closedWith.push(result) };

    TestBed.configureTestingModule({
      providers: [
        { provide: LOCALE_ID, useValue: 'bs' },
        { provide: DEFAULT_CURRENCY_CODE, useValue: 'BAM' },
        {
          provide: MAT_DIALOG_DATA,
          useValue: { publicId: 'rock', name: 'Sarajevo Rock Night', date: '2026-12-09T20:00:00', locationName: 'Zetra' }
        },
        { provide: MatDialogRef, useValue: dialogRef },
        {
          provide: EventService,
          useValue: {
            getTicketTypes: () => {
              loads++;
              offers$ = new Subject<TicketTypeOffer[]>();
              return offers$;
            }
          }
        },
        {
          provide: OrderService,
          useValue: {
            createOrder: (items: OrderLineInput[]) => {
              orders.push(items);
              order$ = new Subject<Order>();
              return order$;
            }
          }
        },
        { provide: ToastService, useValue: { error: (message: string) => toasts.push(message) } }
      ]
    });

    fixture = TestBed.createComponent(BuyTicketsDialogComponent);
    fixture.detectChanges();
  });

  function dialog(): HTMLElement {
    return fixture.nativeElement;
  }

  function show(list = offers): void {
    offers$.next(list);
    fixture.detectChanges();
  }

  function rows(): HTMLElement[] {
    return Array.from(dialog().querySelectorAll<HTMLElement>('.buy__type'));
  }

  function press(row: number, label: 'More' | 'Fewer', times = 1): void {
    for (let i = 0; i < times; i++) {
      rows()[row].querySelector<HTMLButtonElement>(`button[aria-label^="${label}"]`)!.click();
      fixture.detectChanges();
    }
  }

  function quantity(row: number): string {
    return rows()[row].querySelector('.buy__quantity')!.textContent!.trim();
  }

  function reserveButton(): HTMLButtonElement {
    return dialog().querySelector('.ticket__button--confirm')!;
  }

  it('lists the ticket types with what is left and keeps sold-out types closed', () => {
    show();

    expect(rows().map(row => row.querySelector('.buy__type-name')!.textContent!.trim())).toEqual(['Standard', 'VIP', 'Balcony']);
    expect(rows()[0].querySelector('.buy__type-left')).toBeNull();
    expect(rows()[1].querySelector('.buy__type-left')!.textContent).toContain('Only 3 left');
    expect(rows()[2].querySelector('.buy__type-left')!.textContent).toContain('Sold out');
    expect(rows()[2].querySelector<HTMLButtonElement>('button[aria-label^="More"]')!.disabled).toBeTrue();
    expect(reserveButton().disabled).toBeTrue();
  });

  it('caps each quantity at ten and at what is left, and totals the price', () => {
    show();

    press(0, 'More', 12);
    press(1, 'More', 5);

    expect([quantity(0), quantity(1)]).toEqual(['10', '3']);
    expect(dialog().querySelector('.buy__total-label')!.textContent).toContain('13 tickets');
    expect(dialog().querySelector('.buy__total-value')!.textContent).toContain('612,50');

    press(1, 'Fewer');
    expect(quantity(1)).toBe('2');
  });

  it('reserves only the chosen types and closes with the order', () => {
    show();
    press(1, 'More', 2);
    press(0, 'More');
    press(0, 'Fewer');

    reserveButton().click();
    fixture.detectChanges();

    expect(orders).toEqual([[{ ticketTypeId: 'vip', quantity: 2 }]]);
    expect(reserveButton().disabled).toBeTrue();
    expect(dialogRef.disableClose).toBeTrue();

    order$.next({ publicId: 'order-1', status: 'Pending', totalAmount: 175, createdAtUtc: '', items: [] });
    expect(closedWith).toEqual(['order-1']);
  });

  it('shows a refusal, reloads the availability and trims the quantity to it', () => {
    show();
    press(1, 'More', 3);
    reserveButton().click();

    order$.error(new ApiError(409, 'conflict', 'Only 1 "VIP" tickets are left for "Sarajevo Rock Night".'));
    fixture.detectChanges();

    expect(dialog().querySelector('.buy__refusal')!.textContent).toContain('Only 1 "VIP" tickets are left');
    expect(loads).toBe(2);
    expect(dialogRef.disableClose).toBeFalse();

    show(offers.map(offer => (offer.publicId === 'vip' ? { ...offer, available: 1 } : offer)));
    expect(quantity(1)).toBe('1');
    expect(closedWith).toEqual([]);
    expect(toasts).toEqual([]);
  });

  it('reports an unexpected failure as a toast and stays open', () => {
    show();
    press(0, 'More');
    reserveButton().click();

    order$.error(new ApiError(0, 'network', 'Cannot reach the server.'));
    fixture.detectChanges();

    expect(toasts).toEqual(['Cannot reach the server.']);
    expect(loads).toBe(1);
    expect(reserveButton().disabled).toBeFalse();
  });

  it('blocks stale selections while availability is reloading and after a failed reload', () => {
    show();
    press(1, 'More', 3);
    reserveButton().click();
    order$.error(new ApiError(409, 'conflict', 'Only 1 VIP ticket remains.'));
    fixture.detectChanges();

    expect(reserveButton().disabled).toBeTrue();
    expect(rows()[1].querySelector<HTMLButtonElement>('button[aria-label^="More"]')!.disabled).toBeTrue();
    fixture.componentInstance.reserve();
    expect(orders.length).toBe(1);

    offers$.error(new ApiError(0, 'network', 'Cannot reach the server.'));
    fixture.detectChanges();
    expect(rows().length).toBe(0);
    expect(reserveButton().disabled).toBeTrue();
    fixture.componentInstance.reserve();
    expect(orders.length).toBe(1);

    dialog().querySelector<HTMLButtonElement>('.buy__state button')!.click();
    fixture.detectChanges();
    show(offers.map(offer => offer.publicId === 'vip' ? { ...offer, available: 1 } : offer));
    expect(quantity(1)).toBe('1');
    expect(reserveButton().disabled).toBeFalse();
    reserveButton().click();
    expect(orders[1]).toEqual([{ ticketTypeId: 'vip', quantity: 1 }]);
  });

  it('drops a removed ticket type after reload and updates the remaining price and total', () => {
    show();
    press(0, 'More', 2);
    press(1, 'More');
    reserveButton().click();
    order$.error(new ApiError(404, 'not_found', 'The ticket type no longer exists.'));
    fixture.detectChanges();

    show([{ ...offers[0], price: 40, available: 1 }]);
    expect(rows().length).toBe(1);
    expect(quantity(0)).toBe('1');
    expect(dialog().querySelector('.buy__total-label')!.textContent).toContain('1 ticket');
    expect(dialog().querySelector('.buy__total-value')!.textContent).toContain('40,00');
    reserveButton().click();
    expect(orders[1]).toEqual([{ ticketTypeId: 'standard', quantity: 1 }]);
  });

  it('rejects invalid form quantities before sending an order', () => {
    show();
    const form = fixture.debugElement.query(By.directive(FormGroupDirective)).injector.get(FormGroupDirective).form;

    expect(form.invalid).toBeTrue();
    for (const value of [-1, 1.5, 4, 11]) {
      form.get('vip')!.setValue(value);
      fixture.detectChanges();
      expect(form.invalid).toBeTrue();
      expect(reserveButton().disabled).toBeTrue();
      fixture.componentInstance.reserve();
    }
    expect(orders).toEqual([]);
    form.get('vip')!.setValue(2);
    fixture.detectChanges();
    expect(form.valid).toBeTrue();
    expect(reserveButton().disabled).toBeFalse();
  });

  it('offers another try when the ticket types cannot be loaded', () => {
    offers$.error(new ApiError(404, 'not_found', 'Event not found.'));
    fixture.detectChanges();

    expect(dialog().querySelector('.buy__state')!.textContent).toContain('Event not found.');
    dialog().querySelector<HTMLButtonElement>('.buy__state button')!.click();
    fixture.detectChanges();
    show();

    expect(loads).toBe(2);
    expect(rows().length).toBe(3);
  });
});
