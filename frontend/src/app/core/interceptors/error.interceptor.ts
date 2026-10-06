import { HttpContext, HttpContextToken, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, tap, throwError } from 'rxjs';
import { ApiError } from '../models/api-error';
import { ToastService } from '../services/toast.service';

interface ErrorBody {
  code: string;
  message: string;
  errors?: Record<string, string[]>;
}

const fallbackMessages: Record<number, string> = {
  0: 'Cannot reach the server. Check your connection and try again.',
  401: 'Please sign in to continue.',
  403: 'You do not have permission to do this.',
  404: 'The requested resource was not found.'
};

const SHOW_ERROR_TOAST = new HttpContextToken<boolean>(() => true);

// For requests whose caller shows every failure itself, such as a form.
export function withoutErrorToast(): HttpContext {
  return new HttpContext().set(SHOW_ERROR_TOAST, false);
}

export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const toast = inject(ToastService);

  return next(request).pipe(
    catchError((error: unknown) =>
      error instanceof HttpErrorResponse
        ? from(toApiError(error)).pipe(switchMap(apiError => throwError(() => apiError)))
        : throwError(() => error)
    ),
    tap({
      error: (error: unknown) => {
        if (request.context.get(SHOW_ERROR_TOAST) && isUnexpected(error)) {
          toast.error(error.message);
        }
      }
    })
  );
};

function isUnexpected(error: unknown): error is ApiError {
  return error instanceof ApiError && (error.status === 0 || error.status === 403 || error.status === 409 || error.status >= 500);
}

async function toApiError(response: HttpErrorResponse): Promise<ApiError> {
  const body = await readErrorBody(response.error);

  return new ApiError(
    response.status,
    body?.code ?? (response.status === 0 ? 'network_error' : 'request_failed'),
    body?.message || fallbackMessages[response.status] || 'Something went wrong. Please try again.',
    body?.errors ?? {}
  );
}

// Blob downloads deliver their error body as a Blob as well.
async function readErrorBody(error: unknown): Promise<ErrorBody | null> {
  const content = error instanceof Blob ? await error.text() : error;

  if (typeof content === 'string') {
    try {
      return toErrorBody(JSON.parse(content));
    } catch {
      return null;
    }
  }

  return toErrorBody(content);
}

function toErrorBody(value: unknown): ErrorBody | null {
  const body = value as Partial<ErrorBody> | null;
  return typeof body?.code === 'string' && typeof body.message === 'string' ? (body as ErrorBody) : null;
}
