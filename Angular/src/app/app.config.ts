import { ApplicationConfig, DEFAULT_CURRENCY_CODE, LOCALE_ID, provideZoneChangeDetection } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeBs from '@angular/common/locales/bs';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';

registerLocaleData(localeBs);

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    // The error interceptor comes first so it sees the outcome after a refresh and retry.
    provideHttpClient(withInterceptors([errorInterceptor, authInterceptor])),
    { provide: LOCALE_ID, useValue: 'bs' },
    { provide: DEFAULT_CURRENCY_CODE, useValue: 'BAM' }
  ]
};
