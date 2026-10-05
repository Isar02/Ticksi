import { ParamMap, Params } from '@angular/router';
import { CATALOGUE_SORTS, CatalogueQuery } from '../../models/event.model';
import { calendarDate, oneOf, publicIdParam } from '../shared/list-params';

export const SEARCH_MAX_LENGTH = 100;
export const CITY_MAX_LENGTH = 100;

export type CatalogueFilterKey = Exclude<keyof CatalogueQuery, 'sort'>;

// A hand-edited address falls back to defaults instead of reaching the API as a bad request.
export function readCatalogueQuery(params: ParamMap): CatalogueQuery {
  const dateFrom = calendarDate(params.get('dateFrom'));
  const dateTo = calendarDate(params.get('dateTo'));
  const minPrice = price(params.get('minPrice'));
  const maxPrice = price(params.get('maxPrice'));

  return {
    search: text(params.get('search'), SEARCH_MAX_LENGTH),
    categoryId: publicIdParam(params.get('categoryId')),
    locationId: publicIdParam(params.get('locationId')),
    city: text(params.get('city'), CITY_MAX_LENGTH),
    dateFrom,
    dateTo: dateFrom && dateTo && dateTo < dateFrom ? undefined : dateTo,
    minPrice,
    maxPrice: minPrice !== undefined && maxPrice !== undefined && maxPrice < minPrice ? undefined : maxPrice,
    sort: oneOf(params.get('sort'), CATALOGUE_SORTS) ?? CATALOGUE_SORTS[0]
  };
}

export function toCatalogueParams(query: CatalogueQuery): Params {
  return {
    search: query.search ?? null,
    categoryId: query.categoryId ?? null,
    locationId: query.locationId ?? null,
    city: query.city ?? null,
    dateFrom: query.dateFrom ?? null,
    dateTo: query.dateTo ?? null,
    minPrice: query.minPrice ?? null,
    maxPrice: query.maxPrice ?? null,
    sort: query.sort !== CATALOGUE_SORTS[0] ? query.sort : null
  };
}

// A date or price range counts once, as it shows as one chip.
export function activeFilterCount(query: CatalogueQuery): number {
  return [
    query.search,
    query.categoryId,
    query.city,
    query.locationId,
    query.dateFrom ?? query.dateTo,
    query.minPrice ?? query.maxPrice
  ].filter(value => value !== undefined).length;
}

export function withoutFilters(query: CatalogueQuery, ...keys: CatalogueFilterKey[]): CatalogueQuery {
  const next = { ...query };
  for (const key of keys) delete next[key];
  return next;
}

function text(value: string | null, maxLength: number): string | undefined {
  const trimmed = value?.trim();
  return trimmed && trimmed.length <= maxLength ? trimmed : undefined;
}

function price(value: string | null): number | undefined {
  const amount = value === null || value.trim() === '' ? NaN : Number(value);
  return Number.isFinite(amount) && amount >= 0 ? amount : undefined;
}
