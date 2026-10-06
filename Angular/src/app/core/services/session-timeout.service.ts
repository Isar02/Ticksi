import { DestroyRef, Injectable, InjectionToken, Injector, effect, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, filter, fromEvent } from 'rxjs';
import type { SessionWarningData } from '../../components/shared/session-warning-dialog/session-warning-dialog.component';
import { AuthService } from '../../services/auth.service';

export const SESSION_WARNING_MS = 2 * 60_000;

export interface SessionWarningRef {
  close(): void;
  afterClosed(): Observable<unknown>;
}

export type OpenSessionWarning = (data: SessionWarningData) => SessionWarningRef;

// The dialog and its Material code are loaded only when a warning is due, so they stay out of the first download.
export const SESSION_WARNING = new InjectionToken<() => Promise<OpenSessionWarning>>('SESSION_WARNING', {
  providedIn: 'root',
  factory: () => {
    const injector = inject(Injector);
    return async () => {
      const [{ MatDialog }, { SessionWarningDialogComponent }] = await Promise.all([
        import('@angular/material/dialog'),
        import('../../components/shared/session-warning-dialog/session-warning-dialog.component')
      ]);
      const dialog = injector.get(MatDialog);

      return data =>
        dialog.open(SessionWarningDialogComponent, {
          data,
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
    };
  }
});

@Injectable({
  providedIn: 'root'
})
export class SessionTimeoutService {
  private readonly auth = inject(AuthService);
  private readonly loadWarning = inject(SESSION_WARNING);
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);
  private timers: ReturnType<typeof setTimeout>[] = [];
  private warning: SessionWarningRef | null = null;
  private warningDue = false;

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

  // The session may be extended or ended while the dialog loads; then it is not shown.
  private openWarning(): void {
    if (this.warningDue) {
      return;
    }

    this.warningDue = true;
    this.loadWarning().then(
      open => {
        if (!this.warningDue || this.warning) {
          return;
        }

        const warning = open({ warningMs: SESSION_WARNING_MS });
        warning.afterClosed().subscribe(() => {
          if (this.warning === warning) {
            this.warning = null;
            this.warningDue = false;
          }
        });
        this.warning = warning;
      },
      () => (this.warningDue = false)
    );
  }

  private closeWarning(): void {
    this.warningDue = false;
    this.warning?.close();
    this.warning = null;
  }
}
