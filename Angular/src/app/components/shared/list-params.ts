export const PAGE_SIZES = [10, 25, 50] as const;

const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/;
const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function readPage(value: string | null): number {
  const page = Number(value);
  return Number.isInteger(page) && page > 1 && page <= 2147483647 ? page : 1;
}

export function readPageSize(value: string | null): number {
  const pageSize = Number(value);
  return PAGE_SIZES.find(size => size === pageSize) ?? PAGE_SIZES[0];
}

export function publicIdParam(value: string | null): string | undefined {
  return value && GUID.test(value) ? value : undefined;
}

export function calendarDate(value: string | null): string | undefined {
  return value && ISO_DATE.test(value) && !Number.isNaN(Date.parse(value)) && new Date(value).toISOString().startsWith(value)
    ? value
    : undefined;
}

export function oneOf<T extends string>(value: string | null, allowed: readonly T[]): T | undefined {
  return allowed.find(item => item === value);
}
