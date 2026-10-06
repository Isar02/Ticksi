import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleChange, MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { EMPTY, Observable, Subject, catchError, combineLatest, debounceTime, filter, map, of, startWith, switchMap, tap } from 'rxjs';
import { provideIsoDates } from '../../core/dates/iso-date-adapter';
import { ApiError } from '../../core/models/api-error';
import { ToastService } from '../../core/services/toast.service';
import { ROLE_DESCRIPTIONS, RoleOption, UserAccount, UserSort, UsersQuery } from '../../models/user.model';
import { AuthService } from '../../services/auth.service';
import { PagedResult } from '../../services/event.service';
import { UserService } from '../../services/user.service';
import { ConfirmDialogService } from '../shared/confirm-dialog/confirm-dialog.service';
import { endDateNotBeforeStart } from '../shared/form-rules';
import { PAGE_SIZES } from '../shared/list-params';
import { readUsersQuery, toUsersParams } from './users-url';

type Outcome = { page: PagedResult<UserAccount> } | { error: string };
type FilterValues = Pick<UsersQuery, 'search' | 'roleId' | 'status' | 'registeredFrom' | 'registeredTo'>;

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatButtonToggleModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatSortModule,
    MatTableModule,
    MatTooltipModule
  ],
  providers: [provideIsoDates()],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminUsersComponent {
  private readonly users = inject(UserService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new Subject<void>();
  private readonly query$ = this.route.queryParamMap.pipe(map(readUsersQuery));
  private readonly currentUserId = inject(AuthService).currentUser()?.publicId;

  protected readonly columns = ['name', 'email', 'role', 'registered', 'status', 'actions'];
  protected readonly pageSizes = PAGE_SIZES;
  protected readonly skeletonRows = [1, 2, 3, 4, 5];

  protected readonly query = toSignal(this.query$, { requireSync: true });
  protected readonly result = signal<PagedResult<UserAccount> | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  private readonly busyIds = signal<ReadonlySet<string>>(new Set());
  protected readonly roleOptions = signal<RoleOption[]>([]);

  protected readonly hasFilters = computed(() => {
    const query = this.query();
    return !!(query.search || query.roleId || query.status || query.registeredFrom || query.registeredTo);
  });

  protected readonly filters = new FormGroup(
    {
      search: new FormControl('', { nonNullable: true, validators: Validators.maxLength(256) }),
      roleId: new FormControl('', { nonNullable: true }),
      status: new FormControl<UsersQuery['status'] | ''>('', { nonNullable: true }),
      registeredFrom: new FormControl('', { nonNullable: true }),
      registeredTo: new FormControl('', { nonNullable: true })
    },
    { validators: endDateNotBeforeStart('registeredFrom', 'registeredTo') }
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
          this.users.getUsers(query).pipe(
            map((page): Outcome => ({ page })),
            catchError((error: unknown) => of<Outcome>({ error: messageOf(error) }))
          )
        ),
        takeUntilDestroyed()
      )
      .subscribe(outcome => this.show(outcome));

    this.loadRoles();
  }

  protected isSelf(user: UserAccount): boolean {
    return user.publicId === this.currentUserId;
  }

  protected actionsDisabled(user: UserAccount): boolean {
    return this.loading() || !!this.error() || this.isSelf(user) || this.busyIds().has(user.publicId);
  }

  protected initials(user: UserAccount): string {
    return `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase();
  }

  protected roleName(publicId: string): string {
    return this.roleOptions().find(role => role.publicId === publicId)?.name ?? 'All roles';
  }

  protected sortChanged(sort: Sort): void {
    this.navigate({
      ...this.current(),
      sortBy: sort.active as UserSort,
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
    this.loadRoles();
  }

  protected changeRole(user: UserAccount, role: RoleOption): void {
    if (this.actionsDisabled(user) || role.publicId === user.roleId) {
      return;
    }

    const name = fullName(user);
    this.confirmDialog
      .confirm({
        title: `Make ${name} ${withArticle(role.name)}?`,
        message: ROLE_DESCRIPTIONS[role.name] ?? `Their role changes from ${user.roleName} to ${role.name}.`,
        confirmText: `Make ${role.name}`
      })
      .pipe(filter(Boolean), takeUntilDestroyed(this.destroyRef))
      .subscribe(() =>
        this.change(
          user,
          current => this.users.updateUser(current.publicId, {
            firstName: current.firstName,
            lastName: current.lastName,
            email: current.email,
            phone: current.phone,
            roleId: role.publicId,
            isActive: current.isActive
          }),
          `${name} is now ${withArticle(role.name)}.`
        )
      );
  }

  // The toggle shows the saved state until the change is confirmed and stored.
  protected toggleActive(user: UserAccount, change: MatSlideToggleChange): void {
    change.source.checked = user.isActive;
    if (this.actionsDisabled(user)) {
      return;
    }

    if (!user.isActive) {
      this.change(user, current => this.users.setActive(current.publicId, true), `${fullName(user)} can sign in again.`);
      return;
    }

    this.confirmDeactivation(user, {
      title: `Deactivate ${fullName(user)}?`,
      message: 'They cannot sign in or renew their session until the account is activated again. Existing access to some pages may last up to 15 minutes.'
    });
  }

  protected delete(user: UserAccount): void {
    if (this.actionsDisabled(user)) {
      return;
    }

    const name = fullName(user);
    this.confirmDialog
      .confirm({
        title: `Delete ${name}?`,
        message: `The account of ${user.email} and its favorites are removed. This cannot be undone.`,
        confirmText: 'Delete account',
        destructive: true
      })
      .pipe(filter(Boolean), takeUntilDestroyed(this.destroyRef))
      .subscribe(() =>
        this.change(user, current => this.users.deleteUser(current.publicId), `${name} was deleted.`, (error, current) => {
          if (error instanceof ApiError && error.status === 409 && current.isActive) {
            this.confirmDeactivation(current, { title: 'This account cannot be deleted', message: error.message });
          } else {
            this.toast.error(messageOf(error));
          }
        })
      );
  }

  private confirmDeactivation(user: UserAccount, text: { title: string; message: string }): void {
    this.confirmDialog
      .confirm({ ...text, confirmText: 'Deactivate', destructive: true })
      .pipe(filter(Boolean), takeUntilDestroyed(this.destroyRef))
      .subscribe(() =>
        this.change(user, current => this.users.setActive(current.publicId, false), `${fullName(user)} was deactivated. Existing access may last up to 15 minutes.`)
      );
  }

  private change(
    user: UserAccount,
    request: (current: UserAccount) => Observable<void>,
    success: string,
    onError?: (error: unknown, current: UserAccount) => void
  ): void {
    const current = this.result()?.items.find(account => account.publicId === user.publicId);
    if (!current || this.actionsDisabled(current)) {
      return;
    }

    this.setBusy(current.publicId, true);
    request(current)
      .pipe(
        catchError((error: unknown) => {
          this.setBusy(current.publicId, false);
          if (onError) {
            onError(error, current);
          } else {
            this.toast.error(messageOf(error));
          }
          return EMPTY;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        this.setBusy(current.publicId, false);
        this.changed(success);
      });
  }

  private setBusy(publicId: string, busy: boolean): void {
    this.busyIds.update(current => {
      const next = new Set(current);
      if (busy) next.add(publicId);
      else next.delete(publicId);
      return next;
    });
  }

  private changed(message: string): void {
    this.toast.success(message);
    this.reload$.next();
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

  // A failure is already reported by the error interceptor; the role filter and menu just stay empty.
  private loadRoles(): void {
    if (this.roleOptions().length === 0) {
      this.users
        .getRoles()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({ next: roles => this.roleOptions.set(roles), error: () => {} });
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
  private current(): UsersQuery {
    return this.filters.valid ? { ...this.query(), ...this.formFilters() } : this.query();
  }

  private formFilters(): FilterValues {
    const value = this.filters.getRawValue();
    return {
      search: value.search.trim() || undefined,
      roleId: value.roleId || undefined,
      status: value.status || undefined,
      registeredFrom: value.registeredFrom || undefined,
      registeredTo: value.registeredTo || undefined
    };
  }

  private showInForm(query: UsersQuery): void {
    this.filters.patchValue(
      {
        roleId: query.roleId ?? '',
        status: query.status ?? '',
        registeredFrom: query.registeredFrom ?? '',
        registeredTo: query.registeredTo ?? ''
      },
      { emitEvent: false }
    );

    const search = this.filters.controls.search;
    if (search.value.trim() !== (query.search ?? '')) {
      search.setValue(query.search ?? '', { emitEvent: false });
    }
  }

  private navigate(query: UsersQuery, replaceUrl = false): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: toUsersParams(query), replaceUrl });
  }
}

function fullName(user: UserAccount): string {
  return `${user.firstName} ${user.lastName}`;
}

function withArticle(role: string): string {
  return `${/^[aeiou]/i.test(role) ? 'an' : 'a'} ${role}`;
}

function messageOf(error: unknown): string {
  return error instanceof ApiError ? error.message : 'Something went wrong. Please try again.';
}
