import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Ticket } from '../../../models/ticket.model';
import { TicketQrComponent } from '../ticket-qr/ticket-qr.component';

@Component({
  selector: 'app-ticket-card',
  standalone: true,
  imports: [RouterLink, TicketQrComponent],
  templateUrl: './ticket-card.component.html',
  styleUrl: './ticket-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class.ticket--past]': 'past()' }
})
export class TicketCardComponent {
  readonly ticket = input.required<Ticket>();
  readonly past = input(false);

  // Grouped by four so the code is easy to read out at the entrance.
  protected readonly spacedCode = computed(() => this.ticket().code.match(/.{1,4}/g)?.join(' ') ?? '');
  protected readonly orderNumber = computed(() => this.ticket().orderId.slice(0, 8).toUpperCase());
}
