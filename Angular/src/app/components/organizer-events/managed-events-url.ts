import { Params, ParamMap } from '@angular/router';
import { EVENT_PERIODS, MANAGED_EVENT_SORTS, ManagedEventsQuery } from '../../models/event.model';

export const PAGE_SIZES = [10, 25, 50] as const;

const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/;
const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

// A hand-edited address falls back to defaults instead of reaching the API as a bad request.
export function readManagedEventsQuery(params: ParamMap): ManagedEventsQuery {
  const page = Number(params.get('page'));
  const pageSize = Number(params.get('pageSize'));
  const dateFrom = calendarDate(params.get('dateFrom'));
  const dateTo = calendarDate(params.get('dateTo'));

  return {
    name: params.get('name')?.trim() || undefined,
    categoryId: matching(params.get('categoryId'), GUID),
    locationId: matching(params.get('locationId'), GUID),
    dateFrom,
    dateTo: dateFrom && dateTo && dateTo < dateFrom ? undefined : dateTo,
    period: oneOf(params.get('period'), EVENT_PERIODS),
    sortBy: oneOf(params.get('sortBy'), MANAGED_EVENT_SORTS),
    sortDescending: params.get('sortDescending') === 'true' || undefined,
    page: Number.isInteger(page) && page > 1 ? page : 1,
    pageSize: PAGE_SIZES.find(size => size === pageSize) ?? PAGE_SIZES[0]
  };
}

export function toManagedEventsParams(query: ManagedEventsQuery): Params {
  return {
    name: query.name || null,
    categoryId: query.categoryId ?? null,
    locationId: query.locationId ?? null,
    dateFrom: query.dateFrom ?? null,
    dateTo: query.dateTo ?? null,
    period: query.period ?? null,
    sortBy: query.sortBy ?? null,
    sortDescending: query.sortDescending || null,
    page: query.page > 1 ? query.page : null,
    pageSize: query.pageSize !== PAGE_SIZES[0] ? query.pageSize : null
  };
}

function matching(value: string | null, pattern: RegExp): string | undefined {
  return value && pattern.test(value) ? value : undefined;
}

function calendarDate(value: string | null): string | undefined {
  const date = matching(value, ISO_DATE);
  return date && !Number.isNaN(Date.parse(date)) && new Date(date).toISOString().startsWith(date) ? date : undefined;
}

function oneOf<T extends string>(value: string | null, allowed: readonly T[]): T | undefined {
  return allowed.find(item => item === value);
}
