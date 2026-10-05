import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { CategoryReportDialogComponent, CategoryReportDialogData } from './category-report-dialog.component';

@Injectable({
  providedIn: 'root'
})
export class CategoryReportDialogService {
  private readonly dialog = inject(MatDialog);

  open(category: CategoryReportDialogData): void {
    this.dialog.open<CategoryReportDialogComponent, CategoryReportDialogData, boolean>(CategoryReportDialogComponent, {
      data: { publicId: category.publicId, name: category.name },
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
