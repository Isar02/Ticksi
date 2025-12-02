import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment.development';
import { LoginRequest, AuthResponse, UserInfo } from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly TOKEN_KEY = 'ticksi_token';
  private readonly USER_KEY = 'ticksi_user';
  
  private currentUserSubject = new BehaviorSubject<UserInfo | null>(this.getUserFromStorage());
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {}

  // Login wtih password and email
  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, credentials)
      .pipe(
        tap(response => this.handleAuthSuccess(response)),
        catchError(this.handleError)
      );
  }

  // Logout user and clear stored data
  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUserSubject.next(null);
  }

  // Check if user is currently authenticated
  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) {
      return false;
    }

    return !this.isTokenExpired(token);
  }

  
  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  
  getCurrentUser(): UserInfo | null {
    return this.currentUserSubject.value;
  }

  // Successful authentication handler
  private handleAuthSuccess(response: AuthResponse): void {
    const userInfo: UserInfo = {
      email: response.email,
      publicId: response.publicId,
      token: response.token,
      firstName: response.firstName
      
    };

    localStorage.setItem(this.TOKEN_KEY, response.token);
    localStorage.setItem(this.USER_KEY, JSON.stringify(userInfo));
    this.currentUserSubject.next(userInfo);
  }

  
    //Get user from local storage on service init
   
  private getUserFromStorage(): UserInfo | null {
    const userJson = localStorage.getItem(this.USER_KEY);
    if (userJson) {
      try {
        const user = JSON.parse(userJson) as UserInfo;
        if (user.token && !this.isTokenExpired(user.token)) {
          return user;
        }
        // Token expired - clear storage
        this.logout();
      } catch {
        this.logout();
      }
    }
    return null;
  }

  
  private isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const expiry = payload.exp * 1000;
      return Date.now() >= expiry;
    } catch {
      return true;
    }
  }

  // Handle HTTP Errors
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An error occurred. Please try again.';
    
    if (error.error) {
      if (typeof error.error === 'string') {
        errorMessage = error.error;
      } 
      else if (error.error.message) {
        errorMessage = error.error.message;
      } 
      else if (error.error.errors && error.error.errors.length > 0) {
        errorMessage = error.error.errors[0];
      }
    }
    
    return throwError(() => new Error(errorMessage));
  }

  getUserRole(): string | null {
  const token = this.getToken();
  if (!token) return null;

  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return payload.RoleId || null;
  } catch {
    return null;
  }
}

}

