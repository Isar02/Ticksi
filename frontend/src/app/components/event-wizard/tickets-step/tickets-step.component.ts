import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { ErrorStateMatcher } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatTooltipModule } from '@angular/material/tooltip';
import { EVENT_LIMITS, EventWizardForm, TicketTypeForm, createTicketTypeForm, errorText, ticketTotal } from '../event-wizard-form';
import { refreshOnFormEvents } from '../form-events';

@Component({
  selector: 'app-tickets-step',
  standalone: true,
  imports: [DecimalPipe, ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatTooltipModule],
  templateUrl: './tickets-step.component.html',
  styleUrl: './tickets-step.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TicketsStepComponent {
  readonly tickets = input.required<EventWizardForm['controls']['tickets']>();
  readonly capacity = input.required<number | null>();

  protected readonly limits = EVENT_LIMITS;
  protected readonly errorText = errorText;
  private readonly nameMatchers = new WeakMap<TicketTypeForm, ErrorStateMatcher>();

  constructor() {
    refreshOnFormEvents(this.tickets);
  }

  protected add(): void {
    this.tickets().push(createTicketTypeForm());
  }

  protected remove(index: number): void {
    this.tickets().removeAt(index);
  }

  protected total(): number {
    return ticketTotal(this.tickets());
  }

  protected share(): number {
    const capacity = this.capacity();
    return capacity ? Math.min(100, (this.total() / capacity) * 100) : 0;
  }

  protected isDuplicate(ticket: TicketTypeForm): boolean {
    const name = normalized(ticket);
    return !!name && this.tickets().controls.some(other => other !== ticket && normalized(other) === name);
  }

  // A repeated name is an error of the whole list, but it is shown on the rows that share it.
  protected nameMatcher(ticket: TicketTypeForm): ErrorStateMatcher {
    let matcher = this.nameMatchers.get(ticket);
    if (!matcher) {
      matcher = { isErrorState: control => !!control && ((control.invalid && control.touched) || this.isDuplicate(ticket)) };
      this.nameMatchers.set(ticket, matcher);
    }
    return matcher;
  }
}

function normalized(ticket: TicketTypeForm): string {
  return ticket.controls.name.value.trim().toLowerCase();
}
