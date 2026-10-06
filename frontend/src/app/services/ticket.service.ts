import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { withoutErrorToast } from '../core/interceptors/error.interceptor';
import { Ticket } from '../models/ticket.model';

// The tickets page shows its failures itself.
@Injectable({ providedIn: 'root' })
export class TicketService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/tickets`;

  getMine(): Observable<Ticket[]> {
    return this.http.get<Ticket[]>(this.apiUrl, { context: withoutErrorToast() });
  }

  getQrCode(ticketId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${ticketId}/qr`, { responseType: 'blob', context: withoutErrorToast() });
  }
}
