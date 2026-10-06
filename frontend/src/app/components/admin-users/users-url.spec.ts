import { convertToParamMap } from '@angular/router';
import { readUsersQuery, toUsersParams } from './users-url';

describe('users address', () => {
  const roleId = '8d0c3f52-6b1e-4f4a-9c35-1f2e7a9b0c03';

  it('reads every filter, the sort and the page', () => {
    const query = readUsersQuery(
      convertToParamMap({
        search: '  amar ',
        roleId,
        status: 'inactive',
        registeredFrom: '2026-09-01',
        registeredTo: '2026-09-30',
        sortBy: 'registered',
        sortDescending: 'true',
        page: '2',
        pageSize: '50'
      })
    );

    expect(query).toEqual({
      search: 'amar',
      roleId,
      status: 'inactive',
      registeredFrom: '2026-09-01',
      registeredTo: '2026-09-30',
      sortBy: 'registered',
      sortDescending: true,
      page: 2,
      pageSize: 50
    });
  });

  it('falls back to defaults for values the API would refuse', () => {
    const query = readUsersQuery(
      convertToParamMap({
        roleId: 'admin',
        status: 'banned',
        registeredFrom: '2026-02-31',
        registeredTo: '2026-9-1',
        sortBy: 'phone',
        page: '1.5',
        pageSize: '20'
      })
    );

    expect(query).toEqual({
      search: undefined,
      roleId: undefined,
      status: undefined,
      registeredFrom: undefined,
      registeredTo: undefined,
      sortBy: undefined,
      sortDescending: undefined,
      page: 1,
      pageSize: 10
    });
  });

  it('drops an end date before the start date', () => {
    const query = readUsersQuery(convertToParamMap({ registeredFrom: '2026-09-10', registeredTo: '2026-09-01' }));

    expect([query.registeredFrom, query.registeredTo]).toEqual(['2026-09-10', undefined]);
  });

  it('ignores search and page values outside the API limits', () => {
    const invalid = readUsersQuery(convertToParamMap({ search: 'a'.repeat(257), page: '2147483648' }));
    const boundary = readUsersQuery(convertToParamMap({ search: 'a'.repeat(256), page: '2147483647' }));

    expect(invalid.search).toBeUndefined();
    expect(invalid.page).toBe(1);
    expect(boundary.search).toBe('a'.repeat(256));
    expect(boundary.page).toBe(2147483647);
  });

  it('leaves defaults out of the address and survives a round trip', () => {
    const params = toUsersParams({ search: 'amar', status: 'active', sortBy: 'email', page: 1, pageSize: 25 });

    expect(params).toEqual(
      jasmine.objectContaining({ search: 'amar', status: 'active', sortBy: 'email', page: null, pageSize: 25, roleId: null })
    );

    const written = Object.fromEntries(
      Object.entries(params).filter(([, value]) => value !== null).map(([key, value]) => [key, String(value)])
    );
    expect(readUsersQuery(convertToParamMap(written))).toEqual(
      jasmine.objectContaining({ search: 'amar', status: 'active', sortBy: 'email', page: 1, pageSize: 25 })
    );
  });
});
