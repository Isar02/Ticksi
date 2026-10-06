import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { DashboardOrder, orderEventsLabel } from '../../../models/dashboard.model';

@Component({
  selector: 'app-dashboard-orders',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, RouterLink, MatIconModule],
  templateUrl: './dashboard-orders.component.html',
  styleUrl: './dashboard-orders.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardOrdersComponent {
  readonly orders = input.required<DashboardOrder[]>();

  protected readonly label = orderEventsLabel;
}
