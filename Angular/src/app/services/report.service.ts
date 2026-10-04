import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { withoutErrorToast } from '../core/interceptors/error.interceptor';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private apiUrl = `${environment.apiUrl}/reports`;

  constructor(private http: HttpClient) {}

  downloadEventsByCategoryReport(categoryPublicId: string, categoryName: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/events-by-category/${categoryPublicId}`, {
      responseType: 'blob',
      context: withoutErrorToast()
    });
  }

  triggerDownload(blob: Blob, filename: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    window.URL.revokeObjectURL(url);
  }

  generateFilename(categoryName: string): string {
    const sanitized = categoryName
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-|-$/g, '');
    return `events-${sanitized}-report.pdf`;
  }
}
