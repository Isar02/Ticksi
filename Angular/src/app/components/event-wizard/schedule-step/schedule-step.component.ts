import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DecimalPipe } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { eventLocalNow } from '../../../core/dates/event-time';
import { provideIsoDates } from '../../../core/dates/iso-date-adapter';
import { VenueOption } from '../../../models/event.model';
import { EventWizardForm, errorText } from '../event-wizard-form';
import { refreshOnFormEvents } from '../form-events';

@Component({
  selector: 'app-schedule-step',
  standalone: true,
  imports: [DecimalPipe, ReactiveFormsModule, MatDatepickerModule, MatFormFieldModule, MatIconModule, MatInputModule, MatSelectModule],
  providers: [provideIsoDates()],
  templateUrl: './schedule-step.component.html',
  styleUrl: './schedule-step.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ScheduleStepComponent {
  readonly group = input.required<EventWizardForm['controls']['schedule']>();
  readonly venues = input.required<VenueOption[]>();
  readonly selectedVenue = input.required<VenueOption | null>();

  protected readonly today = eventLocalNow().slice(0, 10);
  protected readonly errorText = errorText;

  constructor() {
    refreshOnFormEvents(this.group);
  }
}
