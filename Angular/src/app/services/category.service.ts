import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';
import { Category } from '../models/category.model';
import { AuthService } from './auth.service';

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class CategoryService {

  private apiUrl = `${environment.apiUrl}/eventcategories`;

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

  getPublicCategories(params: {
    search?: string;
    filter?: string;
    page: number;
    pageSize: number;
  }): Observable<PagedResult<Category>> {

    let httpParams = new HttpParams()
      .set('page', params.page)
      .set('pageSize', params.pageSize);

    if (params.search)
      httpParams = httpParams.set('search', params.search);

    if (params.filter)
      httpParams = httpParams.set('filter', params.filter);

    return this.http.get<PagedResult<Category>>(this.apiUrl, { params: httpParams });
  }

  // FOR ADMIN PANEL
  getAll(): Observable<PagedResult<Category>> {
    return this.http.get<PagedResult<Category>>(this.apiUrl);
  }


  create(category: Category): Observable<Category> {
    return this.http.post<Category>(this.apiUrl, category, { headers: this.getAuthHeaders() });
  }

  update(publicId: string, category: Category): Observable<Category> {
    return this.http.put<Category>(`${this.apiUrl}/${publicId}`, category, { headers: this.getAuthHeaders() });
  }

  delete(publicId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${publicId}`, { headers: this.getAuthHeaders() });
  }


}
