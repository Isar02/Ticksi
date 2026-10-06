import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Order, PaymentSession } from '../models/order.model';
import { OrderService } from './order.service';

describe('OrderService', () => {
  let service: OrderService;
  let http: HttpTestingController;

  const order: Order = { publicId: 'order-1', status: 'Pending', totalAmount: 70, createdAtUtc: '2026-10-06T09:33:12.19', items: [] };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(OrderService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('posts the lines and reads the creation time as UTC', () => {
    let created: Order | undefined;
    service.createOrder([{ ticketTypeId: 'standard', quantity: 2 }]).subscribe(result => (created = result));

    const request = http.expectOne(request => request.url.endsWith('/orders'));
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ items: [{ ticketTypeId: 'standard', quantity: 2 }] });
    request.flush(order);

    expect(created!.createdAtUtc).toBe('2026-10-06T09:33:12.19Z');
  });

  it('keeps a creation time that already carries a zone', () => {
    let loaded: Order | undefined;
    service.getOrder('order-1').subscribe(result => (loaded = result));

    http.expectOne(request => request.url.endsWith('/orders/order-1')).flush({ ...order, createdAtUtc: '2026-10-06T09:33:12+02:00' });

    expect(loaded!.createdAtUtc).toBe('2026-10-06T09:33:12+02:00');
  });

  it('starts the payment of the order', () => {
    let session: PaymentSession | undefined;
    service.startPayment('order-1').subscribe(result => (session = result));

    const request = http.expectOne(request => request.url.endsWith('/orders/order-1/payment'));
    expect(request.request.method).toBe('POST');
    const started: PaymentSession = { paid: false, clientSecret: 'pi_1_secret_2', publishableKey: 'pk_test_1', amount: 70, currency: 'BAM' };
    request.flush(started);

    expect(session).toEqual(started);
  });

  it('confirms the payment and reads the creation time as UTC', () => {
    let confirmed: Order | undefined;
    service.confirmPayment('order-1').subscribe(result => (confirmed = result));

    const request = http.expectOne(request => request.url.endsWith('/orders/order-1/payment/confirm'));
    expect(request.request.method).toBe('POST');
    request.flush({ ...order, status: 'Paid' });

    expect(confirmed!.status).toBe('Paid');
    expect(confirmed!.createdAtUtc).toBe('2026-10-06T09:33:12.19Z');
  });
});
