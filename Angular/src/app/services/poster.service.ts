import { Injectable } from '@angular/core';
import { HttpClient, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PosterUploadResponse } from '../models/poster.model';

@Injectable({
  providedIn: 'root'
})
export class PosterService {
  private readonly uploadUrl = `${environment.apiUrl}/posters/upload`;

  constructor(private http: HttpClient) {}

  uploadPoster(file: File): Observable<PosterUploadResponse> {
    return this.http.post<PosterUploadResponse>(this.uploadUrl, toFormData(file));
  }

  uploadPosterWithProgress(file: File): Observable<HttpEvent<PosterUploadResponse>> {
    return this.http.post<PosterUploadResponse>(this.uploadUrl, toFormData(file), {
      reportProgress: true,
      observe: 'events'
    });
  }
}

function toFormData(file: File): FormData {
  const formData = new FormData();
  formData.append('file', file);
  return formData;
}
