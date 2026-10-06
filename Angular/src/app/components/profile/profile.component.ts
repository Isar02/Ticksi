import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Subject, catchError, map, of } from 'rxjs';
import { ToastService } from '../../core/services/toast.service';
import { failureMessage, loadPerSession } from '../../core/utils/load-per-session';
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
  private readonly reload$ = new Subject<void>();

  protected readonly outcome = signal<Outcome | null>(null);

  constructor() {
    loadPerSession('/profile', this.reload$, () =>
      this.profiles.get().pipe(
        map((profile): Outcome => ({ kind: 'loaded', profile })),
        catchError((error: unknown) => of<Outcome>({ kind: 'failed', message: failureMessage(error) }))
      )
    )
      .pipe(takeUntilDestroyed())
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
