import { signal } from '@angular/core';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, Params, Router, convertToParamMap } from '@angular/router';
import { BehaviorSubject, Subject, of } from 'rxjs';
import { CatalogueQuery, Event } from '../../models/event.model';
import { AuthService } from '../../services/auth.service';
import { EventService, PagedResult } from '../../services/event.service';
import { FavoriteService } from '../../services/favorite.service';
import { SearchService, SearchSuggestionDto } from '../../services/search.service';
import { EventsComponent } from './events.component';

describe('EventsComponent', () => {
  const categoryId = '8d246bf9-195f-44b1-871a-9378162bdbbb';
  const venueId = '0d79600c-e8c6-4697-919f-6bcd3614b9a2';

  let address: BehaviorSubject<ParamMap>;
  let requests: { query: CatalogueQuery; page: number; response: Subject<PagedResult<Event>> }[];
  let component: EventsComponent;

  beforeEach(() => {
    address = new BehaviorSubject(convertToParamMap({}));
    requests = [];

    TestBed.configureTestingModule({
      providers: [
        { provide: ActivatedRoute, useValue: { queryParamMap: address } },
        { provide: Router, useValue: { navigate: (_: unknown, extras: { queryParams: Params }) => navigate(extras.queryParams) } },
        { provide: AuthService, useValue: { currentUser: signal(null) } },
        { provide: FavoriteService, useValue: {} },
        { provide: SearchService, useValue: { getSuggestions: () => of([]) } },
        {
          provide: EventService,
          useValue: {
            getCatalogueFilters: () => of({ categories: [{ publicId: categoryId, name: 'Theatre' }], cities: ['Mostar'] }),
            getEvents: (query: CatalogueQuery, page: number) => {
              const response = new Subject<PagedResult<Event>>();
              requests.push({ query, page, response });
              return response;
            }
          }
        }
      ]
    }).overrideComponent(EventsComponent, { set: { template: '<div #sentinel></div>' } });

    component = TestBed.createComponent(EventsComponent).componentInstance;
  });

  function navigate(queryParams: Params): Promise<boolean> {
    const written = Object.entries(queryParams).filter(([, value]) => value !== null && value !== undefined);
    address.next(convertToParamMap(Object.fromEntries(written.map(([key, value]) => [key, String(value)]))));
    return Promise.resolve(true);
  }

  function page(names: string[], pageNumber: number, totalPages: number): PagedResult<Event> {
    return {
      items: names.map(name => ({ publicId: name, name, locationName: `${name} Hall` }) as Event),
      page: pageNumber,
      pageSize: 12,
      totalCount: totalPages * 12,
      totalPages
    };
  }

  function pick(suggestion: Omit<SearchSuggestionDto, 'score'>): void {
    navigate({ search: 'thea' });
    component['pickSuggestion']({ ...suggestion, score: 1 });
  }

  function names(): string[] {
    return component['items']().map(item => item.name);
  }

  it('filters by a picked category instead of searching for its name', () => {
    pick({ type: 'category', label: 'Theatre', publicId: categoryId });

    expect(component['query']()).toEqual(jasmine.objectContaining({ categoryId, search: undefined }));
    expect(requests.at(-1)!.query.search).toBeUndefined();
  });

  it('filters by a picked venue and names it on its chip', () => {
    pick({ type: 'location', label: 'Zetra', publicId: venueId });

    expect(component['query']()).toEqual(jasmine.objectContaining({ locationId: venueId, search: undefined }));
    expect(component['venueName']()).toBe('Zetra');
  });

  it('restores the venue name on Back and Forward before results arrive', () => {
    component['pickSuggestion']({ type: 'location', label: 'Zetra', publicId: venueId, score: 1 });
    component['pickSuggestion']({ type: 'location', label: 'Mostar Arena', publicId: categoryId, score: 1 });

    navigate({ locationId: venueId });
    expect(component['venueName']()).toBe('Zetra');

    navigate({ locationId: categoryId });
    expect(component['venueName']()).toBe('Mostar Arena');
  });

  it('restores the venue name after Clear all even when no events match', () => {
    component['pickSuggestion']({ type: 'location', label: 'Zetra', publicId: venueId, score: 1 });
    component['clearAll']();
    navigate({ locationId: venueId, search: 'no match' });
    requests.at(-1)!.response.next(page([], 1, 0));

    expect(component['venueName']()).toBe('Zetra');
  });

  it('searches for a picked event by its name', () => {
    pick({ type: 'event', label: 'Hamlet', publicId: venueId });

    expect(component['query']()).toEqual(jasmine.objectContaining({ search: 'Hamlet' }));
  });

  it('discards the response of a query that was replaced while it loaded', () => {
    const stale = requests[0];
    navigate({ city: 'Mostar' });
    const current = requests[1];

    stale.response.next(page(['Stale'], 1, 1));
    current.response.next(page(['Fresh'], 1, 1));

    expect(stale.response.observed).toBeFalse();
    expect(current.query.city).toBe('Mostar');
    expect(names()).toEqual(['Fresh']);
  });

  it('appends the next page on request and stops after the last one', () => {
    requests[0].response.next(page(['One'], 1, 2));
    requests[0].response.complete();
    component['loadMore']();
    requests[1].response.next(page(['Two'], 2, 2));
    requests[1].response.complete();
    component['loadMore']();

    expect(requests.map(request => request.page)).toEqual([1, 2]);
    expect(names()).toEqual(['One', 'Two']);
    expect(component['hasMore']()).toBeFalse();
  });

  it('retries the same page after a failure', () => {
    requests[0].response.error(new Error('offline'));
    expect(component['error']()).not.toBeNull();

    component['loadMore']();

    expect(requests.map(request => request.page)).toEqual([1, 1]);
    expect(component['error']()).toBeNull();
  });

  it('applies the filter panel after the pause and skips an inverted price range', fakeAsync(() => {
    const filters = component['filters'];
    filters.patchValue({ city: 'Mostar', minPrice: 50, maxPrice: 10 });
    tick(300);
    expect(component['query']().city).toBeUndefined();

    filters.patchValue({ maxPrice: 80 });
    tick(300);
    expect(component['query']()).toEqual(jasmine.objectContaining({ city: 'Mostar', minPrice: 50, maxPrice: 80 }));
  }));
});
