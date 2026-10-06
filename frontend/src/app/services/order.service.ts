import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { withoutErrorToast } from '../core/interceptors/error.interceptor';
import { asUtcTime } from '../core/utils/utc-time';
import { Order, OrderLineInput, PaymentSession } from '../models/order.model';

// The buy dialog and the order page show their failures themselves.
@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/orders`;

  createOrder(items: OrderLineInput[]): Observable<Order> {
    return this.http.post<Order>(this.apiUrl, { items }, { context: withoutErrorToast() }).pipe(map(withUtcTime));
  }

  getOrder(orderId: string): Observable<Order> {
    return this.http.get<Order>(`${this.apiUrl}/${orderId}`, { context: withoutErrorToast() }).pipe(map(withUtcTime));
  }

  startPayment(orderId: string): Observable<PaymentSession> {
    return this.http.post<PaymentSession>(`${this.apiUrl}/${orderId}/payment`, null, { context: withoutErrorToast() });
  }

  confirmPayment(orderId: string): Observable<Order> {
    return this.http
      .post<Order>(`${this.apiUrl}/${orderId}/payment/confirm`, null, { context: withoutErrorToast() })
      .pipe(map(withUtcTime));
  }
}

function withUtcTime(order: Order): Order {
  return { ...order, createdAtUtc: asUtcTime(order.createdAtUtc) };
}
