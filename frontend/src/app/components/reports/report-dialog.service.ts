import { Injectable, LOCALE_ID, inject } from '@angular/core';
import { formatDate } from '@angular/common';
import { MatDialog } from '@angular/material/dialog';
import { ManagedEvent } from '../../models/event.model';
import { ReportService } from '../../services/report.service';
import { ReportPeriodDialogComponent, ReportPeriodDialogData } from './report-period-dialog/report-period-dialog.component';

export interface ReportCategory {
  publicId: string;
  name: string;
}

@Injectable({
  providedIn: 'root'
})
export class ReportDialogService {
  private readonly dialog = inject(MatDialog);
  private readonly reports = inject(ReportService);
  private readonly locale = inject(LOCALE_ID);

  openCategoryReport(category: ReportCategory): void {
    this.open({
      kind: 'Events report',
      subject: category.name,
      message: 'Choose the period the report covers. Leave a date empty to keep that side open.',
      filename: this.reports.generateFilename('events', category.name),
      download: period => this.reports.downloadEventsByCategoryReport(category.publicId, period)
    });
  }

  openEventSalesReport(event: ManagedEvent): void {
    this.open({
      kind: 'Sales report',
      subject: event.name,
      detail: `${formatDate(event.date, 'shortDate', this.locale)} · ${event.venueName}`,
      message: 'Choose the days of sale the report counts. Leave a date empty to keep that side open.',
      filename: this.reports.generateFilename('sales', event.name),
      download: period => this.reports.downloadEventSalesReport(event.publicId, period)
    });
  }

  private open(data: ReportPeriodDialogData): void {
    this.dialog.open<ReportPeriodDialogComponent, ReportPeriodDialogData, boolean>(ReportPeriodDialogComponent, {
      data,
      width: '480px',
      maxWidth: 'calc(100vw - 32px)',
      panelClass: 'report-dialog-panel',
      autoFocus: 'first-tabbable',
      restoreFocus: true,
      ariaLabelledBy: 'report-dialog-title',
      ariaDescribedBy: 'report-dialog-message'
    });
  }
}
