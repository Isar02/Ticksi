import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { Event } from '../../../models/event.model';
import { BuyTicketsDialogService } from '../buy-tickets-dialog/buy-tickets-dialog.service';
import { isOnSale } from '../on-sale';

@Component({
  selector: 'app-buy-button',
  standalone: true,
  imports: [MatButtonModule],
  templateUrl: './buy-button.component.html',
  styleUrl: './buy-button.component.scss',
  host: { '[class.is-overlay]': 'overlay()' },
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BuyButtonComponent {
  private readonly buyTickets = inject(BuyTicketsDialogService);
  private readonly clockTick = signal(0);

  readonly event = input.required<Event>();
  // Floats over the corner of a poster instead of sitting in the text flow.
  readonly overlay = input(false);

  protected readonly onSale = computed(() => {
    this.clockTick();
    return isOnSale(this.event());
  });

  constructor() {
    effect(onCleanup => {
      this.clockTick();
      const event = this.event();
      const now = Date.now();
      if (!isOnSale(event, now)) return;

      const delay = Math.min(Date.parse(event.date) - now, 86_400_000);
      const timeout = setTimeout(() => this.clockTick.update(value => value + 1), delay);
      onCleanup(() => clearTimeout(timeout));
    });
  }

  protected buy(): void {
    if (!isOnSale(this.event())) {
      this.clockTick.update(value => value + 1);
      return;
    }

    this.buyTickets.open(this.event());
  }
}
