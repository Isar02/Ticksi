import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { Observable, Subject, catchError, forkJoin, map, of, startWith, switchMap, tap } from 'rxjs';
import { ApiError } from '../../core/models/api-error';
import { ToastService } from '../../core/services/toast.service';
import { passwordStrength } from '../../core/utils/password-strength';
import { ROLE_DESCRIPTIONS, RoleOption, USER_LIMITS, UserAccount } from '../../models/user.model';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';
import { applyServerErrors } from '../shared/form-rules';
import { createUserForm, errorText, fillFromUser, toNewUserInput, toUserInput } from './user-form';

interface FormData {
  roles: RoleOption[];
  user: UserAccount | null;
}

type LoadOutcome = { data: FormData } | { error: string } | null;

const DEFAULT_ROLE = 'User';

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSlideToggleModule
  ],
  templateUrl: './user-form.component.html',
  styleUrl: './user-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserFormComponent {
  private readonly users = inject(UserService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new Subject<void>();

  protected readonly userId = inject(ActivatedRoute).snapshot.paramMap.get('id');
  protected readonly isSelf = this.userId !== null && this.userId === inject(AuthService).currentUser()?.publicId;
  protected readonly form = createUserForm(this.userId === null);
  protected readonly limits = USER_LIMITS;
  protected readonly descriptions = ROLE_DESCRIPTIONS;
  protected readonly errorText = errorText;

  protected readonly loaded = signal<LoadOutcome>(null);
  protected readonly saving = signal(false);
  protected readonly passwordHidden = signal(true);

  protected readonly data = computed(() => {
    const outcome = this.loaded();
    return outcome && 'data' in outcome ? outcome.data : null;
  });

  protected readonly loadError = computed(() => {
    const outcome = this.loaded();
    return outcome && 'error' in outcome ? outcome.error : null;
  });

  protected readonly title = computed(() => {
    const user = this.data()?.user;
    if (user) return `${user.firstName} ${user.lastName}`;
    return this.userId ? 'Edit account' : 'Create an account';
  });

  protected readonly strength = toSignal(this.form.controls.password.valueChanges.pipe(map(passwordStrength)), {
    initialValue: null
  });

  constructor() {
    this.reload$
      .pipe(
        startWith(undefined),
        tap(() => this.loaded.set(null)),
        switchMap(() => this.load()),
        takeUntilDestroyed()
      )
      .subscribe(outcome => this.show(outcome));
  }

  protected retry(): void {
    this.reload$.next();
  }

  protected save(): void {
    if (this.saving()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const input = toUserInput(this.form);
    const name = `${input.firstName} ${input.lastName}`;
    const request: Observable<unknown> = this.userId
      ? this.users.updateUser(this.userId, input)
      : this.users.createUser(toNewUserInput(this.form));

    this.saving.set(true);
    this.lock(true);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toast.success(this.userId ? `Changes to ${name} were saved.` : `${name} was created.`);
        this.router.navigateByUrl('/admin/users');
      },
      error: (error: unknown) => {
        this.lock(false);
        this.saving.set(false);
        this.showSaveError(error);
      }
    });
  }

  private load(): Observable<LoadOutcome> {
    return forkJoin({
      roles: this.users.getRoles(),
      user: this.userId ? this.users.getUser(this.userId) : of(null)
    }).pipe(
      map((data): LoadOutcome => ({ data })),
      catchError((error: unknown) => of<LoadOutcome>({ error: messageOf(error) }))
    );
  }

  private show(outcome: LoadOutcome): void {
    if (outcome && 'data' in outcome) {
      const { roles, user } = outcome.data;
      if (user) {
        fillFromUser(this.form, user);
      } else if (!this.form.controls.roleId.value) {
        this.form.controls.roleId.setValue(roles.find(role => role.name === DEFAULT_ROLE)?.publicId ?? '');
      }
      this.lock(false);
    }

    this.loaded.set(outcome);
  }

  // The API refuses changing one's own role or deactivating oneself, so those fields stay locked for the own account.
  private lock(locked: boolean): void {
    if (locked) {
      this.form.disable();
      return;
    }

    this.form.enable();
    if (this.userId) this.form.controls.password.disable();
    if (this.isSelf) {
      this.form.controls.roleId.disable();
      this.form.controls.isActive.disable();
    }
  }

  private showSaveError(error: unknown): void {
    if (!(error instanceof ApiError) || Object.keys(error.fieldErrors).length === 0) {
      this.toast.error(messageOf(error));
      return;
    }

    const unplaced = applyServerErrors(this.form, error.fieldErrors);
    if (unplaced.length > 0) {
      this.toast.error(unplaced.join(' '));
    }
  }
}

function messageOf(error: unknown): string {
  return error instanceof ApiError ? error.message : 'Something went wrong. Please try again.';
}
