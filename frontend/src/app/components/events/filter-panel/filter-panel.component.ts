import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { provideIsoDates } from '../../../core/dates/iso-date-adapter';
import { CatalogueFilters } from '../../../models/event.model';
import { CatalogueFilterForm } from '../catalogue-filter-form';

@Component({
  selector: 'app-filter-panel',
  standalone: true,
  imports: [ReactiveFormsModule, MatButtonModule, MatDatepickerModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  providers: [provideIsoDates()],
  templateUrl: './filter-panel.component.html',
  styleUrl: './filter-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FilterPanelComponent {
  readonly form = input.required<CatalogueFilterForm>();
  readonly options = input.required<CatalogueFilters>();
  readonly filterCount = input(0);
  readonly cleared = output<void>();
}
