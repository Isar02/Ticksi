import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpEvent } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment.development';
import { PosterUploadResponse } from '../models/poster.model';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class CategoryPosterService {

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  /**
   * Upload poster for CATEGORY (no progress)
   */
  uploadPoster(file: File): Observable<PosterUploadResponse> {
    const formData = new FormData();
    formData.append('file', file);

    const token = this.authService.getToken();
    const headers = {
      Authorization: `Bearer ${token}`
    };

    return this.http.post<PosterUploadResponse>(
      `${environment.apiUrl}/categories/poster/upload`,
      formData,
      { headers }
    ).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Upload poster for CATEGORY with progress reporting
   */
  uploadPosterWithProgress(file: File): Observable<HttpEvent<PosterUploadResponse>> {
    const formData = new FormData();
    formData.append('file', file);

    const token = this.authService.getToken();
    const headers = {
      Authorization: `Bearer ${token}`
    };

    return this.http.post<PosterUploadResponse>(
      `${environment.apiUrl}/categories/poster/upload`,
      formData,
      {
        headers,
        reportProgress: true,
        observe: 'events'
      }
    ).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Error handling (isto kao u PosterService)
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An error occurred during file upload. Please try again.';

    if (error.error) {
      if (typeof error.error === 'string') {
        errorMessage = error.error;
      } else if (error.error.message) {
        errorMessage = error.error.message;
      } else if (error.error.errors && error.error.errors.length > 0) {
        errorMessage = error.error.errors[0];
      }
    }

    return throwError(() => new Error(errorMessage));
  }
}
