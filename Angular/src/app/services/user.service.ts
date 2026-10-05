import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { withoutErrorToast } from '../core/interceptors/error.interceptor';
import { NewUserInput, RoleOption, UserAccount, UserInput, UsersQuery } from '../models/user.model';
import { PagedResult } from './event.service';

// The users screens show load and change failures themselves.
@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/users`;

  getUsers(query: UsersQuery): Observable<PagedResult<UserAccount>> {
    const { status, ...rest } = query;
    let params = new HttpParams();
    for (const [key, value] of Object.entries(rest)) {
      if (value !== undefined && value !== '') params = params.set(key, String(value));
    }
    if (status) params = params.set('isActive', String(status === 'active'));

    return this.http.get<PagedResult<UserAccount>>(this.apiUrl, { params, context: withoutErrorToast() });
  }

  getRoles(): Observable<RoleOption[]> {
    return this.http.get<RoleOption[]>(`${this.apiUrl}/roles`);
  }

  getUser(userId: string): Observable<UserAccount> {
    return this.http.get<UserAccount>(`${this.apiUrl}/${userId}`, { context: withoutErrorToast() });
  }

  createUser(input: NewUserInput): Observable<UserAccount> {
    return this.http.post<UserAccount>(this.apiUrl, input, { context: withoutErrorToast() });
  }

  updateUser(userId: string, input: UserInput): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${userId}`, input, { context: withoutErrorToast() });
  }

  setActive(userId: string, isActive: boolean): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${userId}/active`, { isActive }, { context: withoutErrorToast() });
  }

  deleteUser(userId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${userId}`, { context: withoutErrorToast() });
  }
}
