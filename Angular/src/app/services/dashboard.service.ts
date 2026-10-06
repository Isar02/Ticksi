import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { withoutErrorToast } from '../core/interceptors/error.interceptor';
import { asUtcTime } from '../core/utils/utc-time';
import { Dashboard } from '../models/dashboard.model';

// The dashboard shows its failures itself.
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/dashboard`;

  get(): Observable<Dashboard> {
    return this.http.get<Dashboard>(this.apiUrl, { context: withoutErrorToast() }).pipe(
      map(dashboard => ({
        ...dashboard,
        recentOrders: dashboard.recentOrders.map(order => ({ ...order, createdAtUtc: asUtcTime(order.createdAtUtc) }))
      }))
    );
  }
}
