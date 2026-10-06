import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { withoutErrorToast } from '../core/interceptors/error.interceptor';

export interface ReportPeriod {
  dateFrom: string;
  dateTo: string;
}

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private apiUrl = `${environment.apiUrl}/reports`;

  constructor(private http: HttpClient) {}

  downloadEventsByCategoryReport(categoryPublicId: string, period: ReportPeriod): Observable<Blob> {
    return this.downloadPdf(`events-by-category/${categoryPublicId}`, period);
  }

  downloadEventSalesReport(eventPublicId: string, period: ReportPeriod): Observable<Blob> {
    return this.downloadPdf(`event-sales/${eventPublicId}`, period);
  }

  triggerDownload(blob: Blob, filename: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    window.URL.revokeObjectURL(url);
  }

  generateFilename(kind: string, subject: string): string {
    const sanitized = subject
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-|-$/g, '');
    return `${kind}-${sanitized}-report.pdf`;
  }

  private downloadPdf(path: string, period: ReportPeriod): Observable<Blob> {
    let params = new HttpParams();
    if (period.dateFrom) params = params.set('dateFrom', period.dateFrom);
    if (period.dateTo) params = params.set('dateTo', period.dateTo);

    return this.http.get(`${this.apiUrl}/${path}`, {
      params,
      responseType: 'blob',
      context: withoutErrorToast()
    });
  }
}
