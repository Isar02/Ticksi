import { ApplicationConfig, importProvidersFrom, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { MatDialogModule } from '@angular/material/dialog';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [provideZoneChangeDetection({ eventCoalescing: true }),
     provideRouter(routes),
     // The error interceptor comes first so it sees the outcome after a refresh and retry.
     provideHttpClient(withInterceptors([errorInterceptor, authInterceptor])),
     importProvidersFrom(MatDialogModule)]
};
