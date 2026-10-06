import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { EventSummary } from '../event-summary';
import { WizardStep } from '../event-wizard-form';

@Component({
  selector: 'app-review-step',
  standalone: true,
  imports: [CurrencyPipe, DecimalPipe, MatButtonModule, MatIconModule],
  templateUrl: './review-step.component.html',
  styleUrl: './review-step.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReviewStepComponent {
  readonly summary = input.required<EventSummary>();
  readonly poster = input.required<string | null>();
  readonly posterName = input.required<string | null>();
  readonly editStep = output<WizardStep>();
}
