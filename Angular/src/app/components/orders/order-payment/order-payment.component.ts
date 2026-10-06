import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  afterNextRender,
  computed,
  inject,
  input,
  output,
  signal,
  viewChild
} from '@angular/core';
import { CurrencyPipe, DOCUMENT } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Appearance, Stripe, StripeElements, StripePaymentElement } from '@stripe/stripe-js';
import { firstValueFrom } from 'rxjs';
import { ApiError } from '../../../core/models/api-error';
import { StripeLoader } from '../../../core/services/stripe-loader.service';
import { Order } from '../../../models/order.model';
import { OrderService } from '../../../services/order.service';

interface CardForm {
  stripe: Stripe;
  elements: StripeElements;
  element: StripePaymentElement;
}

const FALLBACK_FAILURE = 'The payment could not be started. Please try again.';
const ROBOTO = 'https://fonts.googleapis.com/css2?family=Roboto:wght@400;500&display=swap';

@Component({
  selector: 'app-order-payment',
  standalone: true,
  imports: [CurrencyPipe, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './order-payment.component.html',
  styleUrl: './order-payment.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrderPaymentComponent {
  readonly order = input.required<Order>();
  readonly settled = output<Order>();

  private readonly orders = inject(OrderService);
  private readonly stripeLoader = inject(StripeLoader);
  private readonly document = inject(DOCUMENT);
  private readonly cardHost = viewChild.required<ElementRef<HTMLElement>>('card');

  private form: CardForm | null = null;
  private destroyed = false;

  protected readonly cardReady = signal(false);
  protected readonly paying = signal(false);
  protected readonly failure = signal<string | null>(null);
  protected readonly refusal = signal<string | null>(null);
  protected readonly canPay = computed(() => this.cardReady() && !this.paying() && this.failure() === null);

  constructor() {
    afterNextRender(() => this.start());
    inject(DestroyRef).onDestroy(() => {
      this.destroyed = true;
      this.dropForm();
    });
  }

  protected retry(): void {
    this.start();
  }

  protected async pay(): Promise<void> {
    const form = this.form;
    if (!form || !this.canPay()) return;

    this.paying.set(true);
    this.refusal.set(null);
    form.element.update({ readOnly: true });

    const { error } = await form.stripe.confirmPayment({
      elements: form.elements,
      redirect: 'if_required',
      confirmParams: { return_url: this.returnUrl() }
    });
    if (this.destroyed) return;

    if (error) {
      // Stripe shows card and field errors in the form itself; anything else, such as a lost connection, is explained here.
      const shownByStripe = error.type === 'card_error' || error.type === 'validation_error';
      this.refusal.set(shownByStripe ? null : (error.message ?? 'The payment did not go through. Please try again.'));
      form.element.update({ readOnly: false });
      this.paying.set(false);
      return;
    }

    await this.settle('Your card was charged, but the order could not be updated yet. Please try again.');
  }

  private async start(): Promise<void> {
    this.dropForm();
    this.failure.set(null);
    this.refusal.set(null);
    this.paying.set(false);

    try {
      const session = await firstValueFrom(this.orders.startPayment(this.order().publicId));
      if (session.paid) {
        await this.settle(FALLBACK_FAILURE);
        return;
      }

      const stripe = await this.stripeLoader.load(session.publishableKey!);
      if (!this.destroyed) this.mount(stripe, session.clientSecret!);
    } catch (error) {
      if (this.destroyed) return;
      // Paid or cancelled meanwhile, for example after the bank's check in another tab: show the order as it is now.
      if (error instanceof ApiError && error.status === 409) await this.settle(error.message);
      else this.failure.set(error instanceof ApiError ? error.message : FALLBACK_FAILURE);
    }
  }

  private mount(stripe: Stripe, clientSecret: string): void {
    const elements = stripe.elements({ clientSecret, appearance: this.appearance(), fonts: [{ cssSrc: ROBOTO }], locale: 'en' });
    const element = elements.create('payment', {
      layout: 'tabs',
      business: { name: 'Ticksi' },
      wallets: { applePay: 'never', googlePay: 'never', link: 'never' },
      terms: { card: 'never' }
    });

    element.on('ready', () => this.cardReady.set(true));
    element.on('loaderror', () => {
      this.dropForm();
      this.failure.set('The card form could not be loaded. Please try again.');
    });
    element.mount(this.cardHost().nativeElement);
    this.form = { stripe, elements, element };
  }

  private async settle(pendingMessage: string): Promise<void> {
    try {
      const order = await firstValueFrom(this.orders.confirmPayment(this.order().publicId));
      if (this.destroyed) return;
      if (order.status === 'Pending') {
        this.dropForm();
        this.failure.set(pendingMessage);
      } else {
        this.settled.emit(order);
      }
    } catch (error) {
      if (this.destroyed) return;
      this.dropForm();
      this.failure.set(error instanceof ApiError && error.status !== 409 ? error.message : pendingMessage);
    } finally {
      this.paying.set(false);
    }
  }

  private dropForm(): void {
    this.form?.element.destroy();
    this.form = null;
    this.cardReady.set(false);
  }

  private returnUrl(): string {
    return new URL(`orders/${this.order().publicId}`, this.document.baseURI).href;
  }

  // The card fields live in Stripe's frame, so they take the app's colours from its tokens.
  private appearance(): Appearance {
    const token = (name: string) => getComputedStyle(this.document.documentElement).getPropertyValue(name).trim();
    return {
      theme: 'stripe',
      variables: {
        colorPrimary: token('--tk-brand'),
        colorText: token('--tk-ink'),
        colorTextSecondary: token('--tk-muted'),
        colorDanger: token('--tk-danger'),
        colorBackground: token('--tk-surface'),
        fontFamily: 'Roboto, "Helvetica Neue", sans-serif',
        fontSizeBase: '15px',
        borderRadius: token('--tk-radius-sm'),
        spacingUnit: '4px'
      },
      rules: {
        '.Input': { borderColor: token('--tk-perforation'), boxShadow: 'none' },
        '.Input:focus': { borderColor: token('--tk-brand'), boxShadow: `0 0 0 3px ${token('--tk-brand-soft')}` },
        '.Label': { fontWeight: '500' }
      }
    };
  }
}
