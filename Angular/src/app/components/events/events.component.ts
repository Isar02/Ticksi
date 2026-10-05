import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  afterNextRender,
  computed,
  inject,
  signal,
  viewChild
} from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import {
  EMPTY,
  Observable,
  Subject,
  catchError,
  debounceTime,
  defer,
  exhaustMap,
  filter,
  finalize,
  map,
  of,
  startWith,
  switchMap,
  tap
} from 'rxjs';
import { ApiError } from '../../core/models/api-error';
import { ToastService } from '../../core/services/toast.service';
import { CatalogueFilters, CatalogueQuery, CatalogueSort, Event } from '../../models/event.model';
import { AuthService } from '../../services/auth.service';
import { EventService } from '../../services/event.service';
import { FavoriteService } from '../../services/favorite.service';
import { SearchSuggestionDto } from '../../services/search.service';
import { EventCardComponent } from '../shared/event-card/event-card.component';
import { CatalogueSearchComponent } from './catalogue-search/catalogue-search.component';
import { catalogueFilterForm } from './catalogue-filter-form';
import { FilterPanelComponent } from './filter-panel/filter-panel.component';
import {
  CatalogueFilterKey,
  activeFilterCount,
  readCatalogueQuery,
  toCatalogueParams,
  withoutFilters
} from './catalogue-query';

const PAGE_SIZE = 12;

type FilterValues = Pick<CatalogueQuery, 'categoryId' | 'city' | 'dateFrom' | 'dateTo' | 'minPrice' | 'maxPrice'>;

@Component({
  selector: 'app-events',
  standalone: true,
  imports: [
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatSelectModule,
    CatalogueSearchComponent,
    EventCardComponent,
    FilterPanelComponent
  ],
  templateUrl: './events.component.html',
  styleUrl: './events.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EventsComponent {
  private readonly events = inject(EventService);
  private readonly favorites = inject(FavoriteService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly more$ = new Subject<void>();
  private readonly query$ = this.route.queryParamMap.pipe(map(readCatalogueQuery));
  private readonly sentinel = viewChild.required<ElementRef<HTMLElement>>('sentinel');
  private sentinelObserver?: IntersectionObserver;
  private sentinelVisible = false;

  protected readonly pageSize = PAGE_SIZE;
  protected readonly skeletons = Array.from({ length: 6 }, (_, index) => index);
  protected readonly sorts: { value: CatalogueSort; label: string }[] = [
    { value: 'date-desc', label: 'Date, latest first' },
    { value: 'date-asc', label: 'Date, soonest first' },
    { value: 'price-asc', label: 'Price, low to high' },
    { value: 'price-desc', label: 'Price, high to low' },
    { value: 'name-asc', label: 'Name, A to Z' },
    { value: 'name-desc', label: 'Name, Z to A' }
  ];

  protected readonly query = toSignal(this.query$, { requireSync: true });
  protected readonly items = signal<Event[]>([]);
  protected readonly total = signal<number | null>(null);
  protected readonly hasMore = signal(false);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly options = signal<CatalogueFilters>({ categories: [], cities: [] });
  protected readonly panelOpen = signal(false);
  protected readonly signedIn = computed(() => this.auth.currentUser() !== null);
  protected readonly favoriteIds = signal<ReadonlySet<string>>(new Set());
  protected readonly pendingFavorites = signal<ReadonlySet<string>>(new Set());
  private readonly venueNames = signal<ReadonlyMap<string, string>>(new Map());

  protected readonly filterCount = computed(() => activeFilterCount(this.query()));
  protected readonly categoryName = computed(() => {
    const id = this.query().categoryId;
    return this.options().categories.find(category => category.publicId === id)?.name ?? 'Selected category';
  });
  protected readonly venueName = computed(() =>
    this.venueNames().get(this.query().locationId ?? '') ?? this.items()[0]?.locationName ?? 'Selected venue'
  );
  protected readonly sortControl = new FormControl<CatalogueSort>(this.query().sort, { nonNullable: true });
  protected readonly filters = catalogueFilterForm();

  constructor() {
    this.query$.pipe(takeUntilDestroyed()).subscribe(query => this.showInForm(query));

    this.query$
      .pipe(
        switchMap(query => this.pagesOf(query)),
        takeUntilDestroyed()
      )
      .subscribe();

    this.filters.valueChanges
      .pipe(
        debounceTime(300),
        filter(() => this.filters.valid),
        takeUntilDestroyed()
      )
      .subscribe(() => this.applyFilters());

    this.sortControl.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(sort => this.navigate({ ...this.query(), sort }));

    toObservable(this.auth.currentUser)
      .pipe(
        switchMap(user =>
          user ? this.favorites.getUserFavorites().pipe(catchError(() => of<string[]>([]))) : of<string[]>([])
        ),
        takeUntilDestroyed()
      )
      .subscribe(ids => this.favoriteIds.set(new Set(ids)));

    this.events
      .getCatalogueFilters()
      .pipe(takeUntilDestroyed())
      .subscribe({ next: options => this.options.set(options), error: () => {} });

    afterNextRender(() => this.watchSentinel());
    this.destroyRef.onDestroy(() => this.sentinelObserver?.disconnect());
  }

  protected loadMore(): void {
    this.more$.next();
  }

  protected searchFor(term: string): void {
    this.navigate({ ...this.query(), search: term || undefined }, true);
  }

  protected pickSuggestion(suggestion: SearchSuggestionDto): void {
    const query = { ...this.query(), search: undefined };

    if (suggestion.type === 'category') {
      this.navigate({ ...query, categoryId: suggestion.publicId });
    } else if (suggestion.type === 'location') {
      this.venueNames.update(names => new Map(names).set(suggestion.publicId, suggestion.label));
      this.navigate({ ...query, locationId: suggestion.publicId });
    } else {
      this.navigate({ ...query, search: suggestion.label });
    }
  }

  protected remove(...keys: CatalogueFilterKey[]): void {
    this.navigate(withoutFilters(this.query(), ...keys));
  }

  protected clearAll(): void {
    this.navigate({ sort: this.query().sort });
  }

  protected toggleFavorite(event: Event): void {
    const id = event.publicId;
    if (this.pendingFavorites().has(id)) return;

    const isFavorite = this.favoriteIds().has(id);
    const request = isFavorite ? this.favorites.removeFavorite(id) : this.favorites.addFavorite(id);
    this.pendingFavorites.update(ids => new Set(ids).add(id));

    request
      .pipe(
        finalize(() => this.pendingFavorites.update(ids => without(ids, id))),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => this.favoriteIds.update(ids => (isFavorite ? without(ids, id) : new Set(ids).add(id))),
        error: (error: unknown) => this.toast.error(messageOf(error))
      });
  }

  // Each new query starts an empty list; switchMap drops the responses of the previous one.
  private pagesOf(query: CatalogueQuery): Observable<unknown> {
    return defer(() => {
      let loadedPage = 0;
      this.items.set([]);
      this.total.set(null);
      this.hasMore.set(false);
      this.error.set(null);

      return this.more$.pipe(
        startWith(undefined),
        filter(() => loadedPage === 0 || this.hasMore()),
        exhaustMap(() => {
          this.loading.set(true);
          this.error.set(null);

          return this.events.getEvents(query, loadedPage + 1, this.pageSize).pipe(
            tap(page => {
              loadedPage = page.page;
              this.items.update(items => [...items, ...page.items]);
              this.total.set(page.totalCount);
              this.hasMore.set(page.page < page.totalPages);
              setTimeout(() => this.recheckSentinel());
            }),
            catchError((error: unknown) => {
              this.error.set(messageOf(error));
              return EMPTY;
            }),
            finalize(() => this.loading.set(false))
          );
        })
      );
    });
  }

  private watchSentinel(): void {
    this.sentinelObserver = new IntersectionObserver(
      ([entry]) => {
        this.sentinelVisible = entry.isIntersecting;
        if (this.sentinelVisible && this.hasMore() && !this.loading() && !this.error()) this.more$.next();
      },
      { rootMargin: '0px 0px 600px 0px' }
    );
    this.sentinelObserver.observe(this.sentinel().nativeElement);
  }

  // A short page can leave the sentinel in view, which the observer does not report again.
  private recheckSentinel(): void {
    const element = this.sentinel().nativeElement;
    this.sentinelObserver?.unobserve(element);
    this.sentinelObserver?.observe(element);
  }

  private applyFilters(): void {
    const filters = this.formFilters();
    const query = this.query();

    if ((Object.keys(filters) as (keyof FilterValues)[]).every(key => filters[key] === query[key])) {
      return;
    }

    this.navigate({ ...query, ...filters }, true);
  }

  private formFilters(): FilterValues {
    const value = this.filters.getRawValue();
    return {
      categoryId: value.categoryId || undefined,
      city: value.city || undefined,
      dateFrom: value.dateFrom || undefined,
      dateTo: value.dateTo || undefined,
      minPrice: value.minPrice ?? undefined,
      maxPrice: value.maxPrice ?? undefined
    };
  }

  private showInForm(query: CatalogueQuery): void {
    this.filters.setValue(
      {
        categoryId: query.categoryId ?? '',
        city: query.city ?? '',
        dateFrom: query.dateFrom ?? '',
        dateTo: query.dateTo ?? '',
        minPrice: query.minPrice ?? null,
        maxPrice: query.maxPrice ?? null
      },
      { emitEvent: false }
    );
    this.sortControl.setValue(query.sort, { emitEvent: false });
  }

  private navigate(query: CatalogueQuery, replaceUrl = false): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: toCatalogueParams(query), replaceUrl });
  }
}

function without(ids: ReadonlySet<string>, id: string): Set<string> {
  const next = new Set(ids);
  next.delete(id);
  return next;
}

function messageOf(error: unknown): string {
  return error instanceof ApiError ? error.message : 'Something went wrong. Please try again.';
}
