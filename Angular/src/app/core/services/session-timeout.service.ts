import { DestroyRef, Injectable, Injector, effect, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { filter, fromEvent } from 'rxjs';
import {
  SessionWarningData,
  SessionWarningDialogComponent
} from '../../components/shared/session-warning-dialog/session-warning-dialog.component';
import { AuthService } from '../../services/auth.service';

export const SESSION_WARNING_MS = 2 * 60_000;

@Injectable({
  providedIn: 'root'
})
export class SessionTimeoutService {
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);
  private timers: ReturnType<typeof setTimeout>[] = [];
  private warning: MatDialogRef<SessionWarningDialogComponent> | null = null;

  start(): void {
    effect(() => this.schedule(this.auth.sessionExpiresAt()), { injector: this.injector });

    // Timers in a hidden tab can run late, so the deadline is checked again when the tab comes back.
    fromEvent(document, 'visibilitychange')
      .pipe(
        filter(() => document.visibilityState === 'visible'),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => this.schedule(this.auth.sessionExpiresAt()));

    this.destroyRef.onDestroy(() => this.timers.forEach(clearTimeout));
  }

  private schedule(expiresAt: number | null): void {
    this.timers.forEach(clearTimeout);
    this.timers = [];

    if (expiresAt === null) {
      this.closeWarning();
      return;
    }

    const remaining = expiresAt - Date.now();
    if (remaining > SESSION_WARNING_MS) {
      this.closeWarning();
      this.timers.push(setTimeout(() => this.openWarning(), remaining - SESSION_WARNING_MS));
    } else if (remaining > 0) {
      this.openWarning();
    }

    this.timers.push(setTimeout(() => this.auth.expireIfDue(), Math.max(remaining, 0)));
  }

  private openWarning(): void {
    if (this.warning) {
      return;
    }

    const warning = this.dialog.open<SessionWarningDialogComponent, SessionWarningData>(SessionWarningDialogComponent, {
      data: { warningMs: SESSION_WARNING_MS },
      width: '440px',
      maxWidth: 'calc(100vw - 32px)',
      panelClass: 'session-dialog-panel',
      role: 'alertdialog',
      disableClose: true,
      autoFocus: 'first-tabbable',
      restoreFocus: true,
      ariaLabelledBy: 'session-dialog-title',
      ariaDescribedBy: 'session-dialog-message'
    });
    warning.afterClosed().subscribe(() => {
      if (this.warning === warning) {
        this.warning = null;
      }
    });
    this.warning = warning;
  }

  private closeWarning(): void {
    this.warning?.close();
    this.warning = null;
  }
}
