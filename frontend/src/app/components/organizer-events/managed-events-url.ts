import { Params, ParamMap } from '@angular/router';
import { EVENT_PERIODS, MANAGED_EVENT_SORTS, ManagedEventsQuery } from '../../models/event.model';
import { PAGE_SIZES, calendarDate, oneOf, publicIdParam, readPage, readPageSize } from '../shared/list-params';

// A hand-edited address falls back to defaults instead of reaching the API as a bad request.
export function readManagedEventsQuery(params: ParamMap): ManagedEventsQuery {
  const dateFrom = calendarDate(params.get('dateFrom'));
  const dateTo = calendarDate(params.get('dateTo'));

  return {
    name: params.get('name')?.trim() || undefined,
    categoryId: publicIdParam(params.get('categoryId')),
    locationId: publicIdParam(params.get('locationId')),
    dateFrom,
    dateTo: dateFrom && dateTo && dateTo < dateFrom ? undefined : dateTo,
    period: oneOf(params.get('period'), EVENT_PERIODS),
    sortBy: oneOf(params.get('sortBy'), MANAGED_EVENT_SORTS),
    sortDescending: params.get('sortDescending') === 'true' || undefined,
    page: readPage(params.get('page')),
    pageSize: readPageSize(params.get('pageSize'))
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
