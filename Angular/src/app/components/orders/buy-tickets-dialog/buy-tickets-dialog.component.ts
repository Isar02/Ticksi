import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { AbstractControl, FormControl, FormRecord, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogClose, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiError } from '../../../core/models/api-error';
import { ToastService } from '../../../core/services/toast.service';
import { Event } from '../../../models/event.model';
import { ORDER_MAX_QUANTITY, TicketTypeOffer } from '../../../models/order.model';
import { EventService } from '../../../services/event.service';
import { OrderService } from '../../../services/order.service';

export type BuyTicketsDialogData = Pick<Event, 'publicId' | 'name' | 'date' | 'locationName'>;

const FEW_LEFT = 20;

@Component({
  selector: 'app-buy-tickets-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe, DatePipe, DecimalPipe, MatDialogClose, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './buy-tickets-dialog.component.html',
  styleUrl: './buy-tickets-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BuyTicketsDialogComponent {
  protected readonly event = inject<BuyTicketsDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject<MatDialogRef<BuyTicketsDialogComponent, string>>(MatDialogRef);
  private readonly events = inject(EventService);
  private readonly orders = inject(OrderService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly offers = signal<TicketTypeOffer[] | null>(null);
  protected readonly loading = signal(false);
  protected readonly loadError = signal<string | null>(null);
  protected readonly refusal = signal<string | null>(null);
  protected readonly reserving = signal(false);
  protected readonly quantities = new FormRecord<FormControl<number>>({}, { validators: hasTickets });
  private readonly formEvents = toSignal(this.quantities.events, { initialValue: null });

  protected readonly ticketCount = computed(() => {
    this.formEvents();
    return Object.values(this.quantities.getRawValue()).reduce((sum, count) => sum + count, 0);
  });
  protected readonly total = computed(() =>
    (this.offers() ?? []).reduce((sum, offer) => sum + offer.price * this.quantityOf(offer), 0)
  );
  protected readonly controlsLocked = computed(() => this.loading() || this.loadError() !== null || this.reserving());
  protected readonly canReserve = computed(() => {
    this.formEvents();
    return !this.controlsLocked() && this.quantities.valid;
  });

  constructor() {
    this.load();
  }

  protected load(): void {
    if (this.loading() || this.reserving()) return;

    this.loading.set(true);
    this.loadError.set(null);

    this.events
      .getTicketTypes(this.event.publicId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: offers => {
          this.offers.set(offers);
          this.fitQuantities(offers);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.loadError.set(messageOf(error));
          this.loading.set(false);
        }
      });
  }

  protected quantityOf(offer: TicketTypeOffer): number {
    this.formEvents();
    return this.quantities.controls[offer.publicId]?.value ?? 0;
  }

  protected maxOf(offer: TicketTypeOffer): number {
    return Math.min(ORDER_MAX_QUANTITY, offer.available);
  }

  protected leftLabel(offer: TicketTypeOffer): string | null {
    if (offer.available === 0) return 'Sold out';
    return offer.available <= FEW_LEFT ? `Only ${offer.available} left` : null;
  }

  protected change(offer: TicketTypeOffer, step: 1 | -1): void {
    if (this.controlsLocked()) return;

    const next = Math.min(Math.max(this.quantityOf(offer) + step, 0), this.maxOf(offer));
    this.quantities.controls[offer.publicId]?.setValue(next);
    this.refusal.set(null);
  }

  reserve(): void {
    if (!this.canReserve()) return;

    const items = Object.entries(this.quantities.getRawValue())
      .filter(([, quantity]) => quantity > 0)
      .map(([ticketTypeId, quantity]) => ({ ticketTypeId, quantity }));

    this.setReserving(true);
    this.orders
      .createOrder(items)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: order => this.dialogRef.close(order.publicId),
        error: (error: unknown) => {
          this.setReserving(false);
          if (error instanceof ApiError && (error.status === 409 || error.status === 404)) {
            this.refusal.set(error.message);
            this.load();
          } else {
            this.toast.error(messageOf(error));
          }
        }
      });
  }

  private setReserving(reserving: boolean): void {
    this.reserving.set(reserving);
    this.dialogRef.disableClose = reserving;
  }

  private fitQuantities(offers: TicketTypeOffer[]): void {
    const ids = new Set(offers.map(offer => offer.publicId));
    for (const id of Object.keys(this.quantities.controls)) {
      if (!ids.has(id)) this.quantities.removeControl(id);
    }

    for (const offer of offers) {
      const max = this.maxOf(offer);
      const validators = [Validators.required, wholeNumber, Validators.min(0), Validators.max(max)];
      const control = this.quantities.controls[offer.publicId];
      if (control) {
        control.setValidators(validators);
        control.setValue(Math.min(control.value, max));
      } else {
        this.quantities.addControl(offer.publicId, new FormControl(0, { nonNullable: true, validators }));
      }
    }
  }
}

function hasTickets(control: AbstractControl): ValidationErrors | null {
  return Object.values(control.value).some(quantity => typeof quantity === 'number' && quantity > 0)
    ? null
    : { ticketRequired: true };
}

function wholeNumber(control: AbstractControl<number>): ValidationErrors | null {
  return Number.isInteger(control.value) ? null : { wholeNumber: true };
}

function messageOf(error: unknown): string {
  return error instanceof ApiError ? error.message : 'Something went wrong. Please try again.';
}
