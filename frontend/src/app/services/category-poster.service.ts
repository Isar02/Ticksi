import { Injectable } from '@angular/core';
import { HttpClient, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PosterUploadResponse } from '../models/poster.model';

@Injectable({
  providedIn: 'root'
})
export class CategoryPosterService {

  constructor(private http: HttpClient) {}

  /**
   * Upload poster for CATEGORY (no progress)
   */
  uploadPoster(file: File): Observable<PosterUploadResponse> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<PosterUploadResponse>(
      `${environment.apiUrl}/categories/poster/upload`,
      formData
    );
  }

  /**
   * Upload poster for CATEGORY with progress reporting
   */
  uploadPosterWithProgress(file: File): Observable<HttpEvent<PosterUploadResponse>> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<PosterUploadResponse>(
      `${environment.apiUrl}/categories/poster/upload`,
      formData,
      {
        reportProgress: true,
        observe: 'events'
      }
    );
  }
}
