import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { withoutErrorToast } from '../core/interceptors/error.interceptor';
import { Order, OrderLineInput } from '../models/order.model';

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
}

// The API sends the creation time in UTC without a zone, which the browser would read as local time.
function withUtcTime(order: Order): Order {
  const time = order.createdAtUtc;
  return /(Z|[+-]\d\d:\d\d)$/.test(time) ? order : { ...order, createdAtUtc: `${time}Z` };
}
