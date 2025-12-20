import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment.development';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private apiUrl = `${environment.apiUrl}/reports`;

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

  /**
   * Downloads a PDF report of all events for a specific category
   * @param categoryPublicId The PublicId of the event category
   * @param categoryName The name of the category (for filename)
   */
  downloadEventsByCategoryReport(categoryPublicId: string, categoryName: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/events-by-category/${categoryPublicId}`, {
      headers: this.getAuthHeaders(),
      responseType: 'blob'
    }).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Triggers browser download of a blob as a file
   * @param blob The blob to download
   * @param filename The name for the downloaded file
   */
  triggerDownload(blob: Blob, filename: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    window.URL.revokeObjectURL(url);
  }

  /**
   * Generates a sanitized filename for the PDF report
   * @param categoryName The category name
   */
  generateFilename(categoryName: string): string {
    const sanitized = categoryName
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-|-$/g, '');
    return `events-${sanitized}-report.pdf`;
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An error occurred while generating the report.';
    
    if (error.status === 401) {
      errorMessage = 'Unauthorized. Please log in to download reports.';
    } else if (error.status === 403) {
      errorMessage = 'Forbidden. You do not have permission to download this report.';
    } else if (error.status === 404) {
      errorMessage = 'Category not found or has been deleted.';
    } else if (error.status === 500) {
      errorMessage = 'Server error occurred while generating the report.';
    } else if (error.error instanceof ErrorEvent) {
      errorMessage = `Error: ${error.error.message}`;
    }
    
    console.error('Report download error:', error);
    return throwError(() => new Error(errorMessage));
  }
}

