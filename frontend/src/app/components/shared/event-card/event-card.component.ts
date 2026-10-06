import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Event } from '../../../models/event.model';
import { EventService } from '../../../services/event.service';
import { BuyButtonComponent } from '../../orders/buy-button/buy-button.component';

@Component({
  selector: 'app-event-card',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, RouterLink, MatIconModule, MatTooltipModule, BuyButtonComponent],
  templateUrl: './event-card.component.html',
  styleUrl: './event-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EventCardComponent {
  private readonly events = inject(EventService);

  readonly event = input.required<Event>();
  // Null hides the heart, for visitors who are not signed in.
  readonly favorite = input<boolean | null>(null);
  readonly favoriteBusy = input(false);
  readonly favoriteToggled = output<void>();

  protected readonly posterUrl = computed(() => {
    const url = this.event().posterUrl;
    return url ? this.events.toAssetUrl(url) : null;
  });
}
