import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { withoutErrorToast } from '../core/interceptors/error.interceptor';
import { Event } from '../models/event.model';

@Injectable({
  providedIn: 'root'
})
export class FavoriteService {
  private apiUrl = `${environment.apiUrl}/favorites`;

  constructor(private http: HttpClient) {}

  getUserFavorites(): Observable<string[]> {
    return this.http.get<string[]>(this.apiUrl);
  }

  // The favorites page shows a load failure itself.
  getFavoriteEvents(): Observable<Event[]> {
    return this.http.get<Event[]>(`${this.apiUrl}/events`, { context: withoutErrorToast() });
  }

  addFavorite(eventId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${eventId}`, {});
  }

  removeFavorite(eventId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${eventId}`);
  }
}
