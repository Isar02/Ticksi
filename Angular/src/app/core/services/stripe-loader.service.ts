import { Injectable } from '@angular/core';
import { Stripe } from '@stripe/stripe-js';
import { loadStripe } from '@stripe/stripe-js/pure';

// Stripe.js is fetched only when a payment form first needs it, not on every page.
@Injectable({ providedIn: 'root' })
export class StripeLoader {
  private readonly instances = new Map<string, Promise<Stripe>>();

  load(publishableKey: string): Promise<Stripe> {
    let instance = this.instances.get(publishableKey);
    if (!instance) {
      instance = loadStripe(publishableKey).then(stripe => stripe ?? Promise.reject(new Error('Stripe is not available.')));
      instance.catch(() => this.instances.delete(publishableKey));
      this.instances.set(publishableKey, instance);
    }
    return instance;
  }
}
