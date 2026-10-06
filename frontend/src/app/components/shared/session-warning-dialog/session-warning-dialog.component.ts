import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router } from '@angular/router';
import { interval, map } from 'rxjs';
import { ApiError } from '../../../core/models/api-error';
import { AuthService } from '../../../services/auth.service';

export interface SessionWarningData {
  warningMs: number;
}

const URGENT_MS = 30_000;
const RING_LENGTH = 2 * Math.PI * 32;

@Component({
  selector: 'app-session-warning-dialog',
  standalone: true,
  imports: [MatProgressSpinnerModule],
  templateUrl: './session-warning-dialog.component.html',
  styleUrl: './session-warning-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SessionWarningDialogComponent {
  private readonly data = inject<SessionWarningData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject<MatDialogRef<SessionWarningDialogComponent>>(MatDialogRef);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly now = toSignal(interval(1000).pipe(map(() => Date.now())), { initialValue: Date.now() });

  protected readonly ringLength = RING_LENGTH;
  protected readonly extending = signal(false);
  protected readonly error = signal<string | null>(null);

  private readonly secondsLeft = computed(() =>
    Math.max(0, Math.ceil(((this.auth.sessionExpiresAt() ?? 0) - this.now()) / 1000))
  );

  protected readonly countdown = computed(() => {
    const seconds = this.secondsLeft();
    return `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, '0')}`;
  });

  protected readonly ringOffset = computed(
    () => RING_LENGTH * (1 - Math.min(1, (this.secondsLeft() * 1000) / this.data.warningMs))
  );

  protected readonly urgent = computed(() => this.secondsLeft() * 1000 <= URGENT_MS);

  // Read out at a few steps only, so a screen reader is not interrupted every second.
  protected readonly announcement = computed(() => {
    const seconds = this.secondsLeft();
    if (seconds > 60) return 'You will be signed out in less than two minutes.';
    if (seconds > 30) return 'You will be signed out in less than a minute.';
    if (seconds > 10) return 'You will be signed out in less than 30 seconds.';
    return 'You will be signed out in a few seconds.';
  });

  stay(): void {
    if (this.extending()) return;

    this.extending.set(true);
    this.error.set(null);
    this.auth
      .extendSession()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.dialogRef.close(),
        error: (error: unknown) => {
          this.extending.set(false);
          this.error.set(error instanceof ApiError ? error.message : 'Your session could not be extended. Please try again.');
        }
      });
  }

  signOut(): void {
    this.auth.logout();
    this.dialogRef.close();
    this.router.navigate(['/']);
  }
}
