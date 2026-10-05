import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { withoutErrorToast } from '../core/interceptors/error.interceptor';
import { Event, EventForEdit, EventFormOptions, EventInput, ManagedEvent, ManagedEventsQuery } from '../models/event.model';

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface GetEventsParams {
  search?: string;
  categoryId?: string;
  dateFrom?: string;
  dateTo?: string;
  minPrice?: number;
  maxPrice?: number;
  sortBy?: string;
  sortDescending?: boolean;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class EventService {
  private apiUrl = `${environment.apiUrl}/events`;

  constructor(private http: HttpClient) {}

  getEvents(params: GetEventsParams = {}): Observable<PagedResult<Event>> {
    let httpParams = new HttpParams();

    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.categoryId) httpParams = httpParams.set('categoryId', params.categoryId);

    if (params.dateFrom) httpParams = httpParams.set('dateFrom', params.dateFrom);
    if (params.dateTo) httpParams = httpParams.set('dateTo', params.dateTo);

    if (params.minPrice !== undefined) httpParams = httpParams.set('minPrice', params.minPrice.toString());
    if (params.maxPrice !== undefined) httpParams = httpParams.set('maxPrice', params.maxPrice.toString());

    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDescending !== undefined)
      httpParams = httpParams.set('sortDescending', params.sortDescending.toString());

    if (params.page !== undefined) httpParams = httpParams.set('page', params.page.toString());
    if (params.pageSize !== undefined) httpParams = httpParams.set('pageSize', params.pageSize.toString());

    return this.http.get<PagedResult<Event>>(this.apiUrl, { params: httpParams });
  }

  getEventById(eventId: string): Observable<Event> {
    return this.http.get<Event>(`${this.apiUrl}/${eventId}`);
  }

  getEventImages(eventId: string): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/${eventId}/images`);
  }

  // The organizer screen shows load and delete failures itself.
  getManagedEvents(query: ManagedEventsQuery): Observable<PagedResult<ManagedEvent>> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== '') params = params.set(key, String(value));
    }

    return this.http.get<PagedResult<ManagedEvent>>(`${this.apiUrl}/managed`, { params, context: withoutErrorToast() });
  }

  deleteEvent(eventId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${eventId}`, { context: withoutErrorToast() });
  }

  getFormOptions(): Observable<EventFormOptions> {
    return this.http.get<EventFormOptions>(`${this.apiUrl}/form-options`);
  }

  // The wizard shows load and save failures on the page and on its fields.
  getEventForEdit(eventId: string): Observable<EventForEdit> {
    return this.http.get<EventForEdit>(`${this.apiUrl}/${eventId}/edit`, { context: withoutErrorToast() });
  }

  createEvent(input: EventInput): Observable<Event> {
    return this.http.post<Event>(this.apiUrl, input, { context: withoutErrorToast() });
  }

  updateEvent(eventId: string, input: EventInput): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${eventId}`, input, { context: withoutErrorToast() });
  }

  toAssetUrl(path: string): string {
    if (!path) return '';
    if (/^https?:\/\//i.test(path)) return path;

    const base = environment.apiUrl.replace(/\/api\/?$/i, '');
    return `${base}${path.startsWith('/') ? '' : '/'}${path}`;
  }
}
