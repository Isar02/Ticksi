import { convertToParamMap } from '@angular/router';
import { activeFilterCount, readCatalogueQuery, toCatalogueParams, withoutFilters } from './catalogue-query';

describe('catalogue address', () => {
  const publicId = '8d246bf9-195f-44b1-871a-9378162bdbbb';

  it('reads every filter and the sort', () => {
    const query = readCatalogueQuery(
      convertToParamMap({
        search: '  jazz ',
        categoryId: publicId,
        locationId: publicId,
        city: ' Mostar ',
        dateFrom: '2026-12-01',
        dateTo: '2026-12-31',
        minPrice: '10',
        maxPrice: '25.5',
        sort: 'price-asc'
      })
    );

    expect(query).toEqual({
      search: 'jazz',
      categoryId: publicId,
      locationId: publicId,
      city: 'Mostar',
      dateFrom: '2026-12-01',
      dateTo: '2026-12-31',
      minPrice: 10,
      maxPrice: 25.5,
      sort: 'price-asc'
    });
  });

  it('drops hand-edited values the API would refuse', () => {
    const query = readCatalogueQuery(
      convertToParamMap({
        search: 'a'.repeat(101),
        categoryId: 'music',
        city: '   ',
        dateFrom: '2026-12-31',
        dateTo: '2026-12-01',
        minPrice: '50',
        maxPrice: '10',
        sort: 'popular'
      })
    );

    expect(query).toEqual(jasmine.objectContaining({ dateFrom: '2026-12-31', minPrice: 50, sort: 'date-desc' }));
    expect(query.search).toBeUndefined();
    expect(query.categoryId).toBeUndefined();
    expect(query.city).toBeUndefined();
    expect(query.dateTo).toBeUndefined();
    expect(query.maxPrice).toBeUndefined();
  });

  it('refuses negative, blank and non-numeric prices', () => {
    for (const minPrice of ['-1', ' ', 'ten', 'Infinity']) {
      expect(readCatalogueQuery(convertToParamMap({ minPrice })).minPrice).withContext(minPrice).toBeUndefined();
    }
    expect(readCatalogueQuery(convertToParamMap({ minPrice: '0' })).minPrice).toBe(0);
  });

  it('writes only what is set and leaves the default sort out of the address', () => {
    const params = toCatalogueParams({ city: 'Mostar', minPrice: 0, sort: 'date-desc' });

    expect(Object.entries(params).filter(([, value]) => value !== null)).toEqual([
      ['city', 'Mostar'],
      ['minPrice', 0]
    ]);
    expect(readCatalogueQuery(convertToParamMap({ city: 'Mostar', minPrice: '0' }))).toEqual({
      city: 'Mostar',
      minPrice: 0,
      sort: 'date-desc',
      search: undefined,
      categoryId: undefined,
      locationId: undefined,
      dateFrom: undefined,
      dateTo: undefined,
      maxPrice: undefined
    });
  });

  it('counts a date or price range once and removes filters by key', () => {
    const query = { city: 'Mostar', dateFrom: '2026-12-01', dateTo: '2026-12-31', maxPrice: 40, sort: 'name-asc' as const };

    expect(activeFilterCount(query)).toBe(3);
    expect(withoutFilters(query, 'dateFrom', 'dateTo')).toEqual({ city: 'Mostar', maxPrice: 40, sort: 'name-asc' });
    expect(activeFilterCount({ sort: 'date-desc' })).toBe(0);
  });
});
