import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Subject, catchError, map, of, startWith, switchMap, tap } from 'rxjs';
import { ApiError } from '../../../core/models/api-error';
import { Order, OrderStatus } from '../../../models/order.model';
import { OrderService } from '../../../services/order.service';
import { OrderPaymentComponent } from '../order-payment/order-payment.component';

type Outcome = { kind: 'loaded'; order: Order } | { kind: 'missing' } | { kind: 'failed'; message: string };

const STATUS_LABELS: Record<OrderStatus, string> = {
  Pending: 'Awaiting payment',
  Paid: 'Paid',
  Cancelled: 'Cancelled'
};

@Component({
  selector: 'app-order-details',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, DecimalPipe, RouterLink, MatButtonModule, MatIconModule, OrderPaymentComponent],
  templateUrl: './order-details.component.html',
  styleUrl: './order-details.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrderDetailsComponent {
  private readonly orders = inject(OrderService);
  private readonly route = inject(ActivatedRoute);
  private readonly reload$ = new Subject<void>();

  protected readonly outcome = signal<Outcome | null>(null);
  protected readonly order = computed(() => {
    const outcome = this.outcome();
    return outcome?.kind === 'loaded' ? outcome.order : null;
  });
  protected readonly ticketCount = computed(() => this.order()?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0);
  protected readonly statusLabel = computed(() => {
    const order = this.order();
    return order ? STATUS_LABELS[order.status] : '';
  });

  constructor() {
    this.route.paramMap
      .pipe(
        map(params => params.get('id')!),
        switchMap(id =>
          this.reload$.pipe(
            startWith(undefined),
            tap(() => this.outcome.set(null)),
            switchMap(() =>
              this.orders.getOrder(id).pipe(
                map((order): Outcome => ({ kind: 'loaded', order })),
                catchError((error: unknown) => of<Outcome>(outcomeOf(error)))
              )
            )
          )
        ),
        takeUntilDestroyed()
      )
      .subscribe(outcome => this.outcome.set(outcome));
  }

  protected retry(): void {
    this.reload$.next();
  }

  protected showSettled(order: Order): void {
    this.outcome.set({ kind: 'loaded', order });
  }

  protected orderNumber(order: Order): string {
    return order.publicId.slice(0, 8).toUpperCase();
  }
}

function outcomeOf(error: unknown): Outcome {
  if (error instanceof ApiError && error.status === 404) return { kind: 'missing' };
  return { kind: 'failed', message: error instanceof ApiError ? error.message : 'Something went wrong. Please try again.' };
}
