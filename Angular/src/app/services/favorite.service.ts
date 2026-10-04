import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class FavoriteService {
  private apiUrl = `${environment.apiUrl}/favorites`;

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  private getAuthHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });
  }

  getUserFavorites(): Observable<string[]> {
    return this.http.get<string[]>(this.apiUrl, {
      headers: this.getAuthHeaders()
    });
  }

  addFavorite(eventId: string): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/${eventId}`,
      {},
      { headers: this.getAuthHeaders() }
    );
  }

  removeFavorite(eventId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${eventId}`,
      { headers: this.getAuthHeaders() }
    );
  }
}

