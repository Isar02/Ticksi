export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly code: string,
    message: string,
    readonly fieldErrors: Readonly<Record<string, string[]>> = {}
  ) {
    super(message);
    this.name = 'ApiError';
  }
}
