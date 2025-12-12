import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment.development';
import { PosterUploadResponse } from '../models/poster.model';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class PosterService {
  
  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  /**
   * Upload a poster file to the backend
   * @param file The image file to upload
   * @returns Observable with the upload response containing the file URL
   */
  uploadPoster(file: File): Observable<PosterUploadResponse> {
    const formData = new FormData();
    formData.append('file', file);

    const token = this.authService.getToken();
    const headers = {
      'Authorization': `Bearer ${token}`
    };

    return this.http.post<PosterUploadResponse>(
      `${environment.apiUrl}/posters/upload`, 
      formData,
      { headers }
    ).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Handle HTTP errors
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An error occurred during file upload. Please try again.';
    
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
}

