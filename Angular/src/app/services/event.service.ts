import { Injectable } from '@angular/core';
import { HttpClient, HttpEvent, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { withoutErrorToast } from '../core/interceptors/error.interceptor';
import { CatalogueFilters, CatalogueQuery, Event, EventForEdit, EventFormOptions, EventInput, EventPoster, ManagedEvent, ManagedEventsQuery } from '../models/event.model';

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class EventService {
  private apiUrl = `${environment.apiUrl}/events`;

  constructor(private http: HttpClient) {}

  // The catalogue shows load failures on the page.
  getEvents(query: CatalogueQuery, page: number, pageSize: number): Observable<PagedResult<Event>> {
    const [sortBy, direction] = query.sort.split('-');
    const params = toParams({ ...query, sort: undefined, sortBy, sortDescending: direction === 'desc', page, pageSize });

    return this.http.get<PagedResult<Event>>(this.apiUrl, { params, context: withoutErrorToast() });
  }

  getCatalogueFilters(): Observable<CatalogueFilters> {
    return this.http.get<CatalogueFilters>(`${this.apiUrl}/catalogue-filters`);
  }

  getEventById(eventId: string): Observable<Event> {
    return this.http.get<Event>(`${this.apiUrl}/${eventId}`);
  }

  getEventImages(eventId: string): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/${eventId}/images`);
  }

  // The organizer screen shows load and delete failures itself.
  getManagedEvents(query: ManagedEventsQuery): Observable<PagedResult<ManagedEvent>> {
    return this.http.get<PagedResult<ManagedEvent>>(`${this.apiUrl}/managed`, { params: toParams(query), context: withoutErrorToast() });
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

  uploadPoster(eventId: string, file: File): Observable<HttpEvent<EventPoster>> {
    const body = new FormData();
    body.append('file', file);

    return this.http.put<EventPoster>(`${this.apiUrl}/${eventId}/poster`, body, {
      reportProgress: true,
      observe: 'events',
      context: withoutErrorToast()
    });
  }

  toAssetUrl(path: string): string {
    if (!path) return '';
    if (/^https?:\/\//i.test(path)) return path;

    const base = environment.apiUrl.replace(/\/api\/?$/i, '');
    return `${base}${path.startsWith('/') ? '' : '/'}${path}`;
  }
}

function toParams(query: object): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== '') params = params.set(key, String(value));
  }
  return params;
}
