import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, signal } from '@angular/core';
import { toObservable, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatIconModule } from '@angular/material/icon';
import { Subject, catchError, map, of, startWith, switchMap } from 'rxjs';
import { TicketService } from '../../../services/ticket.service';

type QrState = { kind: 'loading' } | { kind: 'ready'; url: string } | { kind: 'failed' };

@Component({
  selector: 'app-ticket-qr',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './ticket-qr.component.html',
  styleUrl: './ticket-qr.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TicketQrComponent {
  readonly ticketId = input.required<string>();
  readonly code = input.required<string>();

  private readonly tickets = inject(TicketService);
  private readonly retry$ = new Subject<void>();

  protected readonly state = signal<QrState>({ kind: 'loading' });

  constructor() {
    toObservable(this.ticketId)
      .pipe(
        switchMap(id =>
          this.retry$.pipe(
            startWith(undefined),
            switchMap(() =>
              this.tickets.getQrCode(id).pipe(
                map((image): QrState => ({ kind: 'ready', url: URL.createObjectURL(image) })),
                catchError(() => of<QrState>({ kind: 'failed' })),
                startWith<QrState>({ kind: 'loading' })
              )
            )
          )
        ),
        takeUntilDestroyed()
      )
      .subscribe(next => {
        this.release();
        this.state.set(next);
      });

    inject(DestroyRef).onDestroy(() => this.release());
  }

  protected retry(): void {
    this.retry$.next();
  }

  private release(): void {
    const current = this.state();
    if (current.kind === 'ready') URL.revokeObjectURL(current.url);
  }
}
