import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { DecimalPipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { EMPTY, Subject, catchError, map, of, startWith, switchMap, tap } from 'rxjs';
import { loginUrl } from '../../core/guards/return-url';
import { ApiError } from '../../core/models/api-error';
import { Dashboard } from '../../models/dashboard.model';
import { AuthService } from '../../services/auth.service';
import { DashboardService } from '../../services/dashboard.service';
import { DashboardNextEventComponent } from './dashboard-next-event/dashboard-next-event.component';
import { DashboardOrdersComponent } from './dashboard-orders/dashboard-orders.component';
import { DashboardSalesComponent } from './dashboard-sales/dashboard-sales.component';

type Outcome = { kind: 'loaded'; dashboard: Dashboard } | { kind: 'failed'; message: string };

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    DecimalPipe,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    DashboardNextEventComponent,
    DashboardOrdersComponent,
    DashboardSalesComponent
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent {
  private readonly dashboards = inject(DashboardService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly sessionId = computed(() => this.auth.sessionId());
  private readonly reload$ = new Subject<void>();

  protected readonly outcome = signal<Outcome | null>(null);
  protected readonly firstName = computed(() => this.auth.currentUser()?.firstName ?? '');
  protected readonly greeting = greetingFor(new Date());

  constructor() {
    toObservable(this.sessionId)
      .pipe(
        switchMap(sessionId => {
          this.outcome.set(null);
          if (sessionId === null) {
            // Signing out here already navigates away; only a sign-out from another tab is sent to the login page.
            if (!this.router.getCurrentNavigation()) void this.router.navigateByUrl(loginUrl(this.router, '/dashboard'));
            return EMPTY;
          }

          return this.reload$.pipe(
            startWith(undefined),
            tap(() => this.outcome.set(null)),
            switchMap(() =>
              this.dashboards.get().pipe(
                map((dashboard): Outcome => ({ kind: 'loaded', dashboard })),
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
}

export function greetingFor(now: Date): string {
  const hour = now.getHours();
  if (hour < 5 || hour >= 18) return 'Good evening';
  return hour < 12 ? 'Good morning' : 'Good afternoon';
}

function messageOf(error: unknown): string {
  return error instanceof ApiError ? error.message : 'Something went wrong. Please try again.';
}
