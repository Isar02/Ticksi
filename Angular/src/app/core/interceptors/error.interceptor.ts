import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { ApiError } from '../models/api-error';

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

export const errorInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(
    catchError((error: unknown) =>
      error instanceof HttpErrorResponse
        ? from(toApiError(error)).pipe(switchMap(apiError => throwError(() => apiError)))
        : throwError(() => error)
    )
  );

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
