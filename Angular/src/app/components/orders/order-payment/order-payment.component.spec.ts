import { DEFAULT_CURRENCY_CODE, LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeBs from '@angular/common/locales/bs';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';
import { ApiError } from '../../../core/models/api-error';
import { StripeLoader } from '../../../core/services/stripe-loader.service';
import { Order, PaymentSession } from '../../../models/order.model';
import { OrderService } from '../../../services/order.service';
import { ConfirmDialogOptions } from '../../shared/confirm-dialog/confirm-dialog.component';
import { ConfirmDialogService } from '../../shared/confirm-dialog/confirm-dialog.service';
import { OrderPaymentComponent } from './order-payment.component';

describe('OrderPaymentComponent', () => {
  let fixture: ComponentFixture<OrderPaymentComponent>;
  let start: () => Observable<PaymentSession>;
  let confirm: () => Observable<Order>;
  let card: ReturnType<typeof fakeStripe>;
  let loaded: string[];
  let settled: Order[];
  let questions: ConfirmDialogOptions[];
  let answer: boolean;

  const order: Order = {
    publicId: '590d40e2-f31a-483b-9894-5118861eda63',
    status: 'Pending',
    totalAmount: 157.5,
    createdAtUtc: '2026-10-06T09:33:12Z',
    items: []
  };
  const session: PaymentSession = { paid: false, clientSecret: 'pi_1_secret_2', publishableKey: 'pk_test_1', amount: 157.5, currency: 'BAM' };

  beforeAll(() => registerLocaleData(localeBs));

  beforeEach(() => {
    start = () => of(session);
    confirm = () => of({ ...order, status: 'Paid' });
    card = fakeStripe();
    loaded = [];
    settled = [];
    questions = [];
    answer = true;

    TestBed.configureTestingModule({
      providers: [
        { provide: LOCALE_ID, useValue: 'bs' },
        { provide: DEFAULT_CURRENCY_CODE, useValue: 'BAM' },
        { provide: OrderService, useValue: { startPayment: () => start(), confirmPayment: () => confirm() } },
        {
          provide: ConfirmDialogService,
          useValue: {
            confirm: (options: ConfirmDialogOptions) => {
              questions.push(options);
              return of(answer);
            }
          }
        },
        {
          provide: StripeLoader,
          useValue: {
            load: (key: string) => {
              loaded.push(key);
              return Promise.resolve(card.stripe);
            }
          }
        }
      ]
    });
  });

  async function render(value: Order = order): Promise<void> {
    fixture = TestBed.createComponent(OrderPaymentComponent);
    fixture.componentRef.setInput('order', value);
    fixture.componentInstance.settled.subscribe(result => settled.push(result));
    fixture.detectChanges();
    await settle();
  }

  async function settle(): Promise<void> {
    await fixture.whenStable();
    await new Promise(resolve => setTimeout(resolve));
    fixture.detectChanges();
  }

  function page(): HTMLElement {
    return fixture.nativeElement;
  }

  function text(selector: string): string {
    return page().querySelector(selector)?.textContent!.replace(/\s+/g, ' ').trim() ?? '';
  }

  function payButton(): HTMLButtonElement {
    return page().querySelector<HTMLButtonElement>('.pay__button')!;
  }

  async function pay(): Promise<void> {
    payButton().click();
    await settle();
  }

  it('mounts a card-only form that names only Ticksi and enables paying once it is ready', async () => {
    await render();

    expect(loaded).toEqual(['pk_test_1']);
    expect(card.stripe.elements).toHaveBeenCalledWith(jasmine.objectContaining({ clientSecret: 'pi_1_secret_2', locale: 'en' }));
    expect(card.elements.create).toHaveBeenCalledWith('payment', jasmine.objectContaining({
      business: { name: 'Ticksi' },
      wallets: { applePay: 'never', googlePay: 'never', link: 'never' },
      terms: { card: 'never' }
    }));
    expect(card.element.mount).toHaveBeenCalledWith(page().querySelector('.pay__card'));
    expect(text('.pay__amount')).toContain('157,50');
    expect(payButton().disabled).toBeTrue();

    card.handlers['ready']();
    fixture.detectChanges();

    expect(payButton().disabled).toBeFalse();
    expect(text('.pay__button')).toContain('Pay 157,50');
  });

  it('confirms a successful payment with the API and hands the paid order up', async () => {
    await render();
    card.handlers['ready']();
    fixture.detectChanges();

    await pay();

    expect(card.stripe.confirmPayment).toHaveBeenCalledWith(jasmine.objectContaining({
      elements: card.elements,
      redirect: 'if_required',
      confirmParams: { return_url: jasmine.stringMatching(/\/orders\/590d40e2-f31a-483b-9894-5118861eda63$/) }
    }));
    expect(card.element.update).toHaveBeenCalledWith({ readOnly: true });
    expect(settled.map(result => result.status)).toEqual(['Paid']);
  });

  it('asks before charging, with the total, the tickets and every event once', async () => {
    const line = { eventId: 'e1', eventName: 'Jazz Night', eventDate: '2026-11-08T20:00:00', ticketTypeName: 'Standard', quantity: 2, unitPrice: 50 };
    await render({
      ...order,
      items: [line, { ...line, ticketTypeName: 'VIP', quantity: 1, unitPrice: 57.5 }, { ...line, eventId: 'e2', eventName: 'Rock Night', quantity: 1 }]
    });
    card.handlers['ready']();
    fixture.detectChanges();

    await pay();

    expect(card.elements.submit).toHaveBeenCalledBefore(card.stripe.confirmPayment);
    // The locale puts a no-break space between the amount and the currency.
    expect(questions).toEqual([{
      title: 'Pay 157,50 KM?',
      message: 'Your card is charged 157,50 KM for 4 tickets to Jazz Night, Rock Night. A payment cannot be undone.',
      confirmText: 'Pay 157,50 KM',
      cancelText: 'Not yet'
    }]);
    expect(settled.map(result => result.status)).toEqual(['Paid']);
  });

  it('charges nothing and keeps the form open when the buyer is not ready yet', async () => {
    answer = false;
    await render();
    card.handlers['ready']();
    fixture.detectChanges();

    await pay();

    expect(questions.length).toBe(1);
    expect(card.stripe.confirmPayment).not.toHaveBeenCalled();
    expect(card.element.update).not.toHaveBeenCalled();
    expect(payButton().disabled).toBeFalse();
  });

  it('leaves incomplete card fields to Stripe without asking', async () => {
    await render();
    card.handlers['ready']();
    card.elements.submit.and.resolveTo({ error: { type: 'validation_error', message: 'Your card number is incomplete.' } });
    fixture.detectChanges();

    await pay();

    expect(questions).toEqual([]);
    expect(card.stripe.confirmPayment).not.toHaveBeenCalled();
    expect(page().querySelector('.pay__refusal')).toBeNull();
    expect(payButton().disabled).toBeFalse();
  });

  it('lets the buyer try another card after a decline, which Stripe shows in the form', async () => {
    await render();
    card.handlers['ready']();
    card.stripe.confirmPayment.and.resolveTo({ error: { type: 'card_error', message: 'Your card was declined.' } });
    fixture.detectChanges();

    await pay();

    expect(page().querySelector('.pay__refusal')).toBeNull();
    expect(card.element.update).toHaveBeenCalledWith({ readOnly: false });
    expect(payButton().disabled).toBeFalse();
    expect(settled).toEqual([]);

    card.stripe.confirmPayment.and.resolveTo({});
    await pay();

    expect(card.stripe.confirmPayment).toHaveBeenCalledTimes(2);
    expect(settled.map(result => result.status)).toEqual(['Paid']);
  });

  it('explains a failure the form does not show and clears it on the next attempt', async () => {
    await render();
    card.handlers['ready']();
    card.stripe.confirmPayment.and.resolveTo({ error: { type: 'api_connection_error', message: 'We could not connect to Stripe.' } });
    fixture.detectChanges();

    await pay();

    expect(text('.pay__refusal')).toBe('We could not connect to Stripe.');
    expect(payButton().disabled).toBeFalse();

    card.stripe.confirmPayment.and.resolveTo({ error: { type: 'validation_error', message: 'Your card number is incomplete.' } });
    await pay();

    expect(page().querySelector('.pay__refusal')).toBeNull();
    expect(settled).toEqual([]);
  });

  it('issues free tickets only after the buyer clicks and confirms completion', async () => {
    let requests = 0;
    start = () => {
      requests++;
      return of({ ...session, paid: true, clientSecret: null, publishableKey: null, amount: 0 });
    };
    await render({ ...order, totalAmount: 0 });
    expect(requests).toBe(0);
    expect(settled).toEqual([]);
    expect(text('.pay__button')).toContain('Complete order');
    expect(page().querySelector('.pay__field')).toBeNull();

    await pay();

    expect(questions[0].title).toBe('Complete order?');
    expect(requests).toBe(1);
    expect(loaded).toEqual([]);
    expect(settled.map(result => result.status)).toEqual(['Paid']);
  });

  it('leaves free orders pending when completion is declined', async () => {
    let requests = 0;
    start = () => { requests++; return of(session); };
    answer = false;
    await render({ ...order, totalAmount: 0 });
    await pay();
    expect(questions.length).toBe(1);
    expect(requests).toBe(0);
    expect(settled).toEqual([]);
    expect(payButton().disabled).toBeFalse();
  });

  it('requires confirmation again after a failed free completion is retried', async () => {
    let requests = 0;
    start = () => { requests++; return throwError(() => new ApiError(502, 'unavailable', 'Please try again.')); };
    await render({ ...order, totalAmount: 0 });
    await pay();
    expect(requests).toBe(1);
    page().querySelector<HTMLButtonElement>('.pay__failure button')!.click();
    await settle();
    expect(requests).toBe(1);
    answer = false;
    await pay();
    expect(questions.length).toBe(2);
    expect(requests).toBe(1);
    expect(settled).toEqual([]);
  });

  it('shows the order as it is now when it was paid meanwhile', async () => {
    start = () => throwError(() => new ApiError(409, 'conflict', 'This order is already paid.'));

    await render();

    expect(loaded).toEqual([]);
    expect(settled.map(result => result.status)).toEqual(['Paid']);
  });

  it('keeps a cancelled card payment as a message when the order is still pending', async () => {
    start = () => throwError(() => new ApiError(409, 'conflict', 'This payment was cancelled. Please reserve the tickets again.'));
    confirm = () => of(order);

    await render();

    expect(settled).toEqual([]);
    expect(text('.pay__failure')).toContain('This payment was cancelled.');
  });

  it('offers another try when the payment cannot be started', async () => {
    start = () => throwError(() => new ApiError(502, 'payment_unavailable', 'Card payments are unavailable right now.'));

    await render();

    expect(text('.pay__failure p')).toBe('Card payments are unavailable right now.');
    expect(page().querySelector<HTMLElement>('.pay__form')!.hidden).toBeTrue();

    start = () => of(session);
    page().querySelector<HTMLButtonElement>('.pay__failure button')!.click();
    await settle();

    expect(page().querySelector('.pay__failure')).toBeNull();
    expect(card.element.mount).toHaveBeenCalledTimes(1);
  });

  it('asks for another try when the card went through but the order is not updated yet', async () => {
    confirm = () => of(order);
    await render();
    card.handlers['ready']();
    fixture.detectChanges();

    await pay();

    expect(settled).toEqual([]);
    expect(text('.pay__failure p')).toContain('Your card was charged');
    expect(card.element.destroy).toHaveBeenCalled();
  });

  it('removes the card form with the page', async () => {
    await render();

    fixture.destroy();

    expect(card.element.destroy).toHaveBeenCalled();
  });
});

function fakeStripe() {
  const handlers: Record<string, () => void> = {};
  const element = {
    on: (name: string, handler: () => void) => {
      handlers[name] = handler;
      return element;
    },
    mount: jasmine.createSpy('mount'),
    update: jasmine.createSpy('update'),
    destroy: jasmine.createSpy('destroy')
  };
  const elements = {
    create: jasmine.createSpy('create').and.returnValue(element),
    submit: jasmine.createSpy('submit').and.resolveTo({})
  };
  const stripe = {
    elements: jasmine.createSpy('elements').and.returnValue(elements),
    confirmPayment: jasmine.createSpy('confirmPayment').and.resolveTo({})
  };
  return { stripe, elements, element, handlers };
}
