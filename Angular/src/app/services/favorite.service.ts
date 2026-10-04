import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class FavoriteService {
  private apiUrl = `${environment.apiUrl}/favorites`;

  constructor(private http: HttpClient) {}

  getUserFavorites(): Observable<string[]> {
    return this.http.get<string[]>(this.apiUrl);
  }

  addFavorite(eventId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${eventId}`, {});
  }

  removeFavorite(eventId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${eventId}`);
  }
}
