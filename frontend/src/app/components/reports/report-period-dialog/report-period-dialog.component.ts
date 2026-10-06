import { ChangeDetectionStrategy, Component, DestroyRef, LOCALE_ID, computed, inject, signal } from '@angular/core';
import { formatDate } from '@angular/common';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogClose, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Observable } from 'rxjs';
import { provideIsoDates } from '../../../core/dates/iso-date-adapter';
import { ApiError } from '../../../core/models/api-error';
import { ToastService } from '../../../core/services/toast.service';
import { ReportPeriod, ReportService } from '../../../services/report.service';
import { endDateNotBeforeStart } from '../../shared/form-rules';

export interface ReportPeriodDialogData {
  kind: string;
  subject: string;
  detail?: string;
  message: string;
  filename: string;
  download: (period: ReportPeriod) => Observable<Blob>;
}

@Component({
  selector: 'app-report-period-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDatepickerModule, MatDialogClose, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule],
  providers: [provideIsoDates()],
  templateUrl: './report-period-dialog.component.html',
  styleUrl: './report-period-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReportPeriodDialogComponent {
  protected readonly report = inject<ReportPeriodDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject<MatDialogRef<ReportPeriodDialogComponent, boolean>>(MatDialogRef);
  private readonly reports = inject(ReportService);
  private readonly toast = inject(ToastService);
  private readonly locale = inject(LOCALE_ID);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly period = new FormGroup(
    {
      dateFrom: new FormControl('', { nonNullable: true }),
      dateTo: new FormControl('', { nonNullable: true })
    },
    { validators: endDateNotBeforeStart() }
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
    this.report
      .download(this.period.getRawValue())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: blob => {
          this.reports.triggerDownload(blob, this.report.filename);
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
