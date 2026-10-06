import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { NamedOption } from '../../../models/event.model';
import { EVENT_LIMITS, EventWizardForm, errorText } from '../event-wizard-form';
import { refreshOnFormEvents } from '../form-events';

@Component({
  selector: 'app-details-step',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  templateUrl: './details-step.component.html',
  styleUrl: './details-step.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DetailsStepComponent {
  readonly group = input.required<EventWizardForm['controls']['details']>();
  readonly categories = input.required<NamedOption[]>();
  readonly eventTypes = input.required<NamedOption[]>();
  readonly companies = input.required<NamedOption[]>();

  protected readonly limits = EVENT_LIMITS;
  protected readonly errorText = errorText;

  constructor() {
    refreshOnFormEvents(this.group);
  }
}
