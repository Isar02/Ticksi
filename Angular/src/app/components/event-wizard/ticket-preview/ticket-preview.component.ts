import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { EventSummary } from '../event-summary';

@Component({
  selector: 'app-ticket-preview',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, DecimalPipe],
  templateUrl: './ticket-preview.component.html',
  styleUrl: './ticket-preview.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TicketPreviewComponent {
  readonly summary = input.required<EventSummary>();
}
