import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { EMPTY, Subject, catchError, map, of, startWith, switchMap, tap } from 'rxjs';
import { loginUrl } from '../../core/guards/return-url';
import { ApiError } from '../../core/models/api-error';
import { ToastService } from '../../core/services/toast.service';
import { Profile } from '../../models/profile.model';
import { AuthService } from '../../services/auth.service';
import { ProfileService } from '../../services/profile.service';
import { ProfileDetailsComponent } from './profile-details/profile-details.component';
import { ProfilePassComponent } from './profile-pass/profile-pass.component';
import { ProfilePasswordComponent } from './profile-password/profile-password.component';

type Outcome = { kind: 'loaded'; profile: Profile } | { kind: 'failed'; message: string };

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, ProfileDetailsComponent, ProfilePassComponent, ProfilePasswordComponent],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProfileComponent {
  private readonly profiles = inject(ProfileService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly sessionId = computed(() => this.auth.sessionId());
  private readonly reload$ = new Subject<void>();

  protected readonly outcome = signal<Outcome | null>(null);

  constructor() {
    toObservable(this.sessionId)
      .pipe(
        switchMap(sessionId => {
          this.outcome.set(null);
          if (sessionId === null) {
            // Signing out here already navigates away; only a sign-out from another tab is sent to the login page.
            if (!this.router.getCurrentNavigation()) void this.router.navigateByUrl(loginUrl(this.router, '/profile'));
            return EMPTY;
          }

          return this.reload$.pipe(
            startWith(undefined),
            tap(() => this.outcome.set(null)),
            switchMap(() =>
              this.profiles.get().pipe(
                map((profile): Outcome => ({ kind: 'loaded', profile })),
                catchError((error: unknown) => of<Outcome>({ kind: 'failed', message: messageOf(error) }))
              )
            )
          );
        }),
        takeUntilDestroyed()
      )
      .subscribe(outcome => this.outcome.set(outcome));
  }

  protected retry(): void {
    this.reload$.next();
  }

  protected saved(profile: Profile): void {
    this.outcome.set({ kind: 'loaded', profile });
    this.auth.renameUser(profile.firstName);
    this.toast.success('Your details have been saved.');
  }

  protected passwordChanged(): void {
    this.toast.success('Your password has been changed. Your other devices have been signed out.');
  }
}

function messageOf(error: unknown): string {
  return error instanceof ApiError ? error.message : 'Something went wrong. Please try again.';
}
