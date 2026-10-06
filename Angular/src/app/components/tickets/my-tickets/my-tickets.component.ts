import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe, DecimalPipe, NgTemplateOutlet } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Subject, catchError, map, of } from 'rxjs';
import { failureMessage, loadPerSession } from '../../../core/utils/load-per-session';
import { Ticket } from '../../../models/ticket.model';
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
    loadPerSession('/tickets', this.reload$, () =>
      this.tickets.getMine().pipe(
        map((tickets): Outcome => ({ kind: 'loaded', tickets })),
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
