import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Subject, catchError, map, of } from 'rxjs';
import { failureMessage, loadPerSession } from '../../core/utils/load-per-session';
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
  private readonly reload$ = new Subject<void>();

  protected readonly outcome = signal<Outcome | null>(null);
  protected readonly firstName = computed(() => this.auth.currentUser()?.firstName ?? '');
  protected readonly greeting = greetingFor(new Date());

  constructor() {
    loadPerSession('/dashboard', this.reload$, () =>
      this.dashboards.get().pipe(
        map((dashboard): Outcome => ({ kind: 'loaded', dashboard })),
        catchError((error: unknown) => of<Outcome>({ kind: 'failed', message: failureMessage(error) }))
      )
    )
      .pipe(takeUntilDestroyed())
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
