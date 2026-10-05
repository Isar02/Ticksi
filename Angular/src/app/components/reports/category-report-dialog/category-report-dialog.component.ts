import { ChangeDetectionStrategy, Component, DestroyRef, LOCALE_ID, computed, inject, signal } from '@angular/core';
import { formatDate } from '@angular/common';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogClose, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiError } from '../../../core/models/api-error';
import { ToastService } from '../../../core/services/toast.service';
import { ReportService } from '../../../services/report.service';
import { endDateNotBeforeStart } from '../../shared/form-rules';

export interface CategoryReportDialogData {
  publicId: string;
  name: string;
}

@Component({
  selector: 'app-category-report-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogClose, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule],
  templateUrl: './category-report-dialog.component.html',
  styleUrl: './category-report-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CategoryReportDialogComponent {
  protected readonly category = inject<CategoryReportDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject<MatDialogRef<CategoryReportDialogComponent, boolean>>(MatDialogRef);
  private readonly reports = inject(ReportService);
  private readonly toast = inject(ToastService);
  private readonly locale = inject(LOCALE_ID);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly period = new FormGroup(
    {
      dateFrom: new FormControl('', { nonNullable: true }),
      dateTo: new FormControl('', { nonNullable: true })
    },
    { validators: endDateNotBeforeStart }
  );

  protected readonly downloading = signal(false);

  private readonly range = toSignal(this.period.valueChanges, { initialValue: this.period.getRawValue() });

  protected readonly covers = computed(() => {
    const from = this.range().dateFrom ? this.format(this.range().dateFrom!) : '';
    const to = this.range().dateTo ? this.format(this.range().dateTo!) : '';

    if (from && to) return `${from} – ${to}`;
    if (from) return `From ${from}`;
    if (to) return `Until ${to}`;
    return 'All dates';
  });

  download(): void {
    if (this.period.invalid || this.downloading()) return;

    this.setDownloading(true);
    this.reports
      .downloadEventsByCategoryReport(this.category.publicId, this.period.getRawValue())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: blob => {
          this.reports.triggerDownload(blob, this.reports.generateFilename(this.category.name));
          this.dialogRef.close(true);
        },
        error: (error: ApiError) => {
          this.toast.error(error.message);
          this.setDownloading(false);
        }
      });
  }

  private setDownloading(downloading: boolean): void {
    this.downloading.set(downloading);
    this.dialogRef.disableClose = downloading;
    if (downloading) {
      this.period.disable({ emitEvent: false });
    } else {
      this.period.enable({ emitEvent: false });
    }
  }

  private format(date: string): string {
    return formatDate(date, 'shortDate', this.locale);
  }
}
