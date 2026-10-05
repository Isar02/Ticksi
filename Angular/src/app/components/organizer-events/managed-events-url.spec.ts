import { convertToParamMap } from '@angular/router';
import { readManagedEventsQuery, toManagedEventsParams } from './managed-events-url';

describe('managed events address', () => {
  const publicId = '8d246bf9-195f-44b1-871a-9378162bdbbb';

  it('reads every filter, the sort and the page', () => {
    const query = readManagedEventsQuery(
      convertToParamMap({
        name: '  jazz ',
        categoryId: publicId,
        locationId: publicId,
        dateFrom: '2026-12-01',
        dateTo: '2026-12-31',
        period: 'upcoming',
        sortBy: 'sold',
        sortDescending: 'true',
        page: '3',
        pageSize: '25'
      })
    );

    expect(query).toEqual({
      name: 'jazz',
      categoryId: publicId,
      locationId: publicId,
      dateFrom: '2026-12-01',
      dateTo: '2026-12-31',
      period: 'upcoming',
      sortBy: 'sold',
      sortDescending: true,
      page: 3,
      pageSize: 25
    });
  });

  it('falls back to defaults for values the API would refuse', () => {
    const query = readManagedEventsQuery(
      convertToParamMap({
        categoryId: 'abc',
        dateFrom: '2026-13',
        period: 'soon',
        sortBy: 'price',
        sortDescending: 'yes',
        page: '-2',
        pageSize: '500'
      })
    );

    expect(query).toEqual({
      name: undefined,
      categoryId: undefined,
      locationId: undefined,
      dateFrom: undefined,
      dateTo: undefined,
      period: undefined,
      sortBy: undefined,
      sortDescending: undefined,
      page: 1,
      pageSize: 10
    });
  });

  it('drops an end date before the start date and dates that do not exist', () => {
    const reversed = readManagedEventsQuery(convertToParamMap({ dateFrom: '2026-12-10', dateTo: '2026-12-01' }));
    const impossible = readManagedEventsQuery(convertToParamMap({ dateFrom: '2026-02-31', dateTo: '2026-12-01' }));

    expect([reversed.dateFrom, reversed.dateTo]).toEqual(['2026-12-10', undefined]);
    expect([impossible.dateFrom, impossible.dateTo]).toEqual([undefined, '2026-12-01']);
  });

  it('leaves defaults out of the address and survives a round trip', () => {
    const params = toManagedEventsParams({ name: 'jazz', sortBy: 'venue', sortDescending: true, page: 2, pageSize: 10 });

    expect(params).toEqual(jasmine.objectContaining({ name: 'jazz', sortBy: 'venue', sortDescending: true, page: 2, pageSize: null }));
    expect(params['period']).toBeNull();

    const written = Object.fromEntries(
      Object.entries(params).filter(([, value]) => value !== null).map(([key, value]) => [key, String(value)])
    );
    expect(readManagedEventsQuery(convertToParamMap(written))).toEqual(
      jasmine.objectContaining({ name: 'jazz', sortBy: 'venue', sortDescending: true, page: 2, pageSize: 10 })
    );
  });
});
