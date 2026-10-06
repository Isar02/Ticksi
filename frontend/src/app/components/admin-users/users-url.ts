import { Params, ParamMap } from '@angular/router';
import { USER_SORTS, USER_STATUSES, UsersQuery } from '../../models/user.model';
import { PAGE_SIZES, calendarDate, oneOf, publicIdParam, readPage, readPageSize } from '../shared/list-params';

// A hand-edited address falls back to defaults instead of reaching the API as a bad request.
export function readUsersQuery(params: ParamMap): UsersQuery {
  const search = params.get('search')?.trim();
  const registeredFrom = calendarDate(params.get('registeredFrom'));
  const registeredTo = calendarDate(params.get('registeredTo'));

  return {
    search: search && search.length <= 256 ? search : undefined,
    roleId: publicIdParam(params.get('roleId')),
    status: oneOf(params.get('status'), USER_STATUSES),
    registeredFrom,
    registeredTo: registeredFrom && registeredTo && registeredTo < registeredFrom ? undefined : registeredTo,
    sortBy: oneOf(params.get('sortBy'), USER_SORTS),
    sortDescending: params.get('sortDescending') === 'true' || undefined,
    page: readPage(params.get('page')),
    pageSize: readPageSize(params.get('pageSize'))
  };
}

export function toUsersParams(query: UsersQuery): Params {
  return {
    search: query.search || null,
    roleId: query.roleId ?? null,
    status: query.status ?? null,
    registeredFrom: query.registeredFrom ?? null,
    registeredTo: query.registeredTo ?? null,
    sortBy: query.sortBy ?? null,
    sortDescending: query.sortDescending || null,
    page: query.page > 1 ? query.page : null,
    pageSize: query.pageSize !== PAGE_SIZES[0] ? query.pageSize : null
  };
}
