import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { withoutErrorToast } from '../core/interceptors/error.interceptor';
import { asUtcTime } from '../core/utils/utc-time';
import { Profile, ProfileInput } from '../models/profile.model';

// The profile page shows its failures itself.
@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/profile`;

  get(): Observable<Profile> {
    return this.http.get<Profile>(this.apiUrl, { context: withoutErrorToast() }).pipe(map(withUtcDate));
  }

  update(input: ProfileInput): Observable<Profile> {
    return this.http.put<Profile>(this.apiUrl, input, { context: withoutErrorToast() }).pipe(map(withUtcDate));
  }
}

function withUtcDate(profile: Profile): Profile {
  return { ...profile, registrationDate: asUtcTime(profile.registrationDate) };
}
