import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';
import { Event } from '../models/event.model';

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

  getEvents(params: {
    search?: string;
    categoryId?: string;
    dateFrom?: string;
    dateTo?: string;
    minPrice?: number;
    maxPrice?: number;
    sortBy?: string;
    sortDescending?: boolean;
    page: number;
    pageSize: number;
  }): Observable<PagedResult<Event>> {

    let httpParams = new HttpParams()
      .set('page', params.page)
      .set('pageSize', params.pageSize);

    if (params.search)
      httpParams = httpParams.set('search', params.search);

    if (params.categoryId)
      httpParams = httpParams.set('categoryId', params.categoryId);

    if (params.dateFrom)
      httpParams = httpParams.set('dateFrom', params.dateFrom);

    if (params.dateTo)
      httpParams = httpParams.set('dateTo', params.dateTo);

    if (params.minPrice !== undefined)
      httpParams = httpParams.set('minPrice', params.minPrice.toString());

    if (params.maxPrice !== undefined)
      httpParams = httpParams.set('maxPrice', params.maxPrice.toString());

    if (params.sortBy)
      httpParams = httpParams.set('sortBy', params.sortBy);

    if (params.sortDescending !== undefined)
      httpParams = httpParams.set('sortDescending', params.sortDescending.toString());

    return this.http.get<PagedResult<Event>>(this.apiUrl, { params: httpParams });
  }
}

