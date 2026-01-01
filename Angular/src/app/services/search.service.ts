import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment.development';
import { Observable } from 'rxjs';

export type SuggestionType = 'event' | 'category' | 'location';

export interface SearchSuggestionDto {
  type: SuggestionType;
  label: string;
  publicId: string; // Guid from backend comes as string in JSON
  score: number;
}

@Injectable({ providedIn: 'root' })
export class SearchService {
  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getSuggestions(q: string, limit = 10): Observable<SearchSuggestionDto[]> {
    const params = new HttpParams()
      .set('q', q)
      .set('limit', String(limit));

    return this.http.get<SearchSuggestionDto[]>(
      `${this.baseUrl}/search/suggestions`,
      { params }
    );
  }
}
