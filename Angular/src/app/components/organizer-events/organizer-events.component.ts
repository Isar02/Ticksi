import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { DatePipe, DecimalPipe } from '@angular/common';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, catchError, combineLatest, debounceTime, filter, map, of, startWith, switchMap, tap } from 'rxjs';
import { ApiError } from '../../core/models/api-error';
import { Role } from '../../core/models/role';
import { ToastService } from '../../core/services/toast.service';
import { Category } from '../../models/category.model';
import { EventPeriod, ManagedEvent, ManagedEventSort, ManagedEventsQuery, VenueOption } from '../../models/event.model';
import { AuthService } from '../../services/auth.service';
import { CategoryService } from '../../services/category.service';
import { EventService, PagedResult } from '../../services/event.service';
import { ReportDialogService } from '../reports/report-dialog.service';
import { ConfirmDialogService } from '../shared/confirm-dialog/confirm-dialog.service';
import { PAGE_SIZES } from '../shared/list-params';
import { readManagedEventsQuery, toManagedEventsParams } from './managed-events-url';

type Outcome = { page: PagedResult<ManagedEvent> } | { error: string };
type FilterValues = Pick<ManagedEventsQuery, 'name' | 'categoryId' | 'locationId' | 'dateFrom' | 'dateTo' | 'period'>;

@Component({
  selector: 'app-organizer-events',
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatButtonToggleModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
    MatSortModule,
    MatTableModule,
    MatTooltipModule
  ],
  templateUrl: './organizer-events.component.html',
  styleUrl: './organizer-events.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrganizerEventsComponent {
  private readonly events = inject(EventService);
  private readonly categories = inject(CategoryService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly reportDialog = inject(ReportDialogService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new Subject<void>();
  private readonly query$ = this.route.queryParamMap.pipe(map(readManagedEventsQuery));

  protected readonly columns = ['name', 'date', 'venue', 'sold', 'actions'];
  protected readonly pageSizes = PAGE_SIZES;
  protected readonly skeletonRows = [1, 2, 3, 4, 5];
  protected readonly isAdmin = inject(AuthService).hasAnyRole([Role.Admin]);

  protected readonly query = toSignal(this.query$, { requireSync: true });
  protected readonly result = signal<PagedResult<ManagedEvent> | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly deletingId = signal<string | null>(null);
  protected readonly categoryOptions = signal<Category[]>([]);
  protected readonly venueOptions = signal<VenueOption[]>([]);

  protected readonly hasFilters = computed(() => {
    const query = this.query();
    return !!(query.name || query.categoryId || query.locationId || query.dateFrom || query.dateTo || query.period);
  });

  protected readonly filters = new FormGroup(
    {
      name: new FormControl('', { nonNullable: true, validators: Validators.maxLength(200) }),
      categoryId: new FormControl('', { nonNullable: true }),
      locationId: new FormControl('', { nonNullable: true }),
      dateFrom: new FormControl('', { nonNullable: true }),
      dateTo: new FormControl('', { nonNullable: true }),
      period: new FormControl<EventPeriod | ''>('', { nonNullable: true })
    },
    { validators: endNotBeforeStart }
  );

  constructor() {
    this.query$.pipe(takeUntilDestroyed()).subscribe(query => this.showInForm(query));

    this.filters.valueChanges
      .pipe(
        debounceTime(300),
        filter(() => this.filters.valid),
        takeUntilDestroyed()
      )
      .subscribe(() => this.applyFilters());

    combineLatest([this.query$, this.reload$.pipe(startWith(undefined))])
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.error.set(null);
        }),
        switchMap(([query]) =>
          this.events.getManagedEvents(query).pipe(
            map((page): Outcome => ({ page })),
            catchError((error: unknown) => of<Outcome>({ error: messageOf(error) }))
          )
        ),
        takeUntilDestroyed()
      )
      .subscribe(outcome => this.show(outcome));

    this.loadFilterOptions();
  }

  protected sortChanged(sort: Sort): void {
    this.navigate({
      ...this.current(),
      sortBy: sort.active as ManagedEventSort,
      sortDescending: sort.direction === 'desc' || undefined,
      page: 1
    });
  }

  protected pageChanged(event: PageEvent): void {
    this.navigate({ ...this.current(), page: event.pageIndex + 1, pageSize: event.pageSize });
  }

  protected clearFilters(): void {
    const { sortBy, sortDescending, pageSize } = this.query();
    this.navigate({ sortBy, sortDescending, pageSize, page: 1 });
  }

  protected retry(): void {
    this.reload$.next();
    this.loadFilterOptions();
  }

  protected venueName(publicId: string): string {
    return this.venueOptions().find(venue => venue.publicId === publicId)?.name ?? 'All venues';
  }

  protected ticketsSoldShare(event: ManagedEvent): number {
    return event.ticketsTotal > 0 ? Math.min(100, (event.ticketsSold / event.ticketsTotal) * 100) : 0;
  }

  protected openSalesReport(event: ManagedEvent): void {
    this.reportDialog.openEventSalesReport(event);
  }

  protected delete(event: ManagedEvent): void {
    this.confirmDialog
      .confirm({
        title: 'Delete this event?',
        message: `"${event.name}" and its ticket types will be removed. This cannot be undone.`,
        confirmText: 'Delete event',
        destructive: true
      })
      .pipe(
        filter(Boolean),
        tap(() => this.deletingId.set(event.publicId)),
        switchMap(() => this.events.deleteEvent(event.publicId)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.deletingId.set(null);
          this.toast.success(`"${event.name}" was deleted.`);
          this.reload$.next();
        },
        error: (error: unknown) => {
          this.deletingId.set(null);
          this.toast.error(messageOf(error));
        }
      });
  }

  private show(outcome: Outcome): void {
    this.loading.set(false);

    if ('error' in outcome) {
      this.error.set(outcome.error);
      return;
    }

    const { page } = outcome;
    if (page.items.length === 0 && page.page > 1 && page.totalCount > 0) {
      this.navigate({ ...this.query(), page: page.totalPages }, true);
      return;
    }

    this.result.set(page);
  }

  // A failure is already reported by the error interceptor; the filter just stays without options.
  private loadFilterOptions(): void {
    if (this.categoryOptions().length === 0) {
      this.categories
        .getPublicCategories({ page: 1, pageSize: 50 })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({ next: page => this.categoryOptions.set(page.items), error: () => {} });
    }

    if (this.venueOptions().length === 0) {
      this.events
        .getFormOptions()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({ next: options => this.venueOptions.set(options.venues), error: () => {} });
    }
  }

  private applyFilters(): void {
    const filters = this.formFilters();
    const query = this.query();

    if ((Object.keys(filters) as (keyof FilterValues)[]).every(key => filters[key] === query[key])) {
      return;
    }

    this.navigate({ ...query, ...filters, page: 1 }, true);
  }

  // Filters still inside the debounce go along, so a quick sort or page click does not drop them.
  private current(): ManagedEventsQuery {
    return this.filters.valid ? { ...this.query(), ...this.formFilters() } : this.query();
  }

  private formFilters(): FilterValues {
    const value = this.filters.getRawValue();
    return {
      name: value.name.trim() || undefined,
      categoryId: value.categoryId || undefined,
      locationId: value.locationId || undefined,
      dateFrom: value.dateFrom || undefined,
      dateTo: value.dateTo || undefined,
      period: value.period || undefined
    };
  }

  private showInForm(query: ManagedEventsQuery): void {
    this.filters.patchValue(
      {
        categoryId: query.categoryId ?? '',
        locationId: query.locationId ?? '',
        dateFrom: query.dateFrom ?? '',
        dateTo: query.dateTo ?? '',
        period: query.period ?? ''
      },
      { emitEvent: false }
    );

    // The name being typed keeps its trailing space and caret when the address catches up with it.
    const name = this.filters.controls.name;
    if (name.value.trim() !== (query.name ?? '')) {
      name.setValue(query.name ?? '', { emitEvent: false });
    }
  }

  private navigate(query: ManagedEventsQuery, replaceUrl = false): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: toManagedEventsParams(query), replaceUrl });
  }
}

function endNotBeforeStart(group: AbstractControl): ValidationErrors | null {
  const { dateFrom, dateTo } = group.value as { dateFrom: string; dateTo: string };
  return dateFrom && dateTo && dateTo < dateFrom ? { dateRange: true } : null;
}

function messageOf(error: unknown): string {
  return error instanceof ApiError ? error.message : 'Something went wrong. Please try again.';
}
