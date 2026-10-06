import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { DatePipe, DecimalPipe, NgTemplateOutlet } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { EMPTY, Subject, catchError, map, of, startWith, switchMap, tap } from 'rxjs';
import { loginUrl } from '../../../core/guards/return-url';
import { ApiError } from '../../../core/models/api-error';
import { Ticket } from '../../../models/ticket.model';
import { AuthService } from '../../../services/auth.service';
import { TicketService } from '../../../services/ticket.service';
import { TicketWallet, groupByEvent } from '../ticket-groups';
import { TicketCardComponent } from '../ticket-card/ticket-card.component';

type Outcome = { kind: 'loaded'; tickets: Ticket[] } | { kind: 'failed'; message: string };

@Component({
  selector: 'app-my-tickets',
  standalone: true,
  imports: [DatePipe, DecimalPipe, NgTemplateOutlet, RouterLink, MatButtonModule, MatIconModule, TicketCardComponent],
  templateUrl: './my-tickets.component.html',
  styleUrl: './my-tickets.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MyTicketsComponent {
  private readonly tickets = inject(TicketService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  // The session id stays the same when tokens rotate, but changes on a new sign-in.
  private readonly sessionId = computed(() => this.auth.sessionId());
  private readonly reload$ = new Subject<void>();

  protected readonly skeletons = [1, 2, 3];
  protected readonly outcome = signal<Outcome | null>(null);
  protected readonly wallet = computed<TicketWallet | null>(() => {
    const outcome = this.outcome();
    return outcome?.kind === 'loaded' ? groupByEvent(outcome.tickets, new Date()) : null;
  });
  protected readonly ticketCount = computed(() => {
    const outcome = this.outcome();
    return outcome?.kind === 'loaded' ? outcome.tickets.length : 0;
  });

  constructor() {
    toObservable(this.sessionId)
      .pipe(
        switchMap(sessionId => {
          this.outcome.set(null);
          if (sessionId === null) {
            // Signing out here already navigates away; only a sign-out from another tab is sent to the login page.
            if (!this.router.getCurrentNavigation()) void this.router.navigateByUrl(loginUrl(this.router, '/tickets'));
            return EMPTY;
          }

          return this.reload$.pipe(
            startWith(undefined),
            tap(() => this.outcome.set(null)),
            switchMap(() =>
              this.tickets.getMine().pipe(
                map((tickets): Outcome => ({ kind: 'loaded', tickets })),
                catchError((error: unknown) =>
                  of<Outcome>({ kind: 'failed', message: error instanceof ApiError ? error.message : 'Something went wrong. Please try again.' })
                )
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
