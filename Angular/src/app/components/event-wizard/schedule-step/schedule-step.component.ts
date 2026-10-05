import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DecimalPipe } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { VenueOption } from '../../../models/event.model';
import { EventWizardForm, errorText } from '../event-wizard-form';
import { refreshOnFormEvents } from '../form-events';

@Component({
  selector: 'app-schedule-step',
  standalone: true,
  imports: [DecimalPipe, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  templateUrl: './schedule-step.component.html',
  styleUrl: './schedule-step.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ScheduleStepComponent {
  readonly group = input.required<EventWizardForm['controls']['schedule']>();
  readonly venues = input.required<VenueOption[]>();
  readonly selectedVenue = input.required<VenueOption | null>();

  protected readonly today = toLocalIsoDate(new Date());
  protected readonly errorText = errorText;

  constructor() {
    refreshOnFormEvents(this.group);
  }
}

function toLocalIsoDate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}
