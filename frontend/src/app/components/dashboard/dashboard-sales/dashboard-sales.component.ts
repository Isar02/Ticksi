import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { DashboardSales, DashboardTopEvent } from '../../../models/dashboard.model';

@Component({
  selector: 'app-dashboard-sales',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, DecimalPipe, RouterLink, MatButtonModule, MatIconModule],
  templateUrl: './dashboard-sales.component.html',
  styleUrl: './dashboard-sales.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardSalesComponent {
  readonly sales = input.required<DashboardSales>();

  private readonly bestSold = computed(() => Math.max(0, ...this.sales().topEvents.map(event => event.ticketsSold)));

  protected share(event: DashboardTopEvent): number {
    return this.bestSold() > 0 ? (event.ticketsSold / this.bestSold()) * 100 : 0;
  }
}
