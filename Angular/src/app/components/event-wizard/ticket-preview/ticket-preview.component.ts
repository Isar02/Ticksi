import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { EventSummary } from '../event-summary';

@Component({
  selector: 'app-ticket-preview',
  standalone: true,
  imports: [CurrencyPipe, DecimalPipe],
  templateUrl: './ticket-preview.component.html',
  styleUrl: './ticket-preview.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TicketPreviewComponent {
  readonly summary = input.required<EventSummary>();
}
