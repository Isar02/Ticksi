import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { DashboardEvent } from '../../../models/dashboard.model';

@Component({
  selector: 'app-dashboard-next-event',
  standalone: true,
  imports: [DatePipe, RouterLink, MatButtonModule, MatIconModule],
  templateUrl: './dashboard-next-event.component.html',
  styleUrl: './dashboard-next-event.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardNextEventComponent {
  readonly event = input.required<DashboardEvent | null>();
}
