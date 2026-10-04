import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable, map } from 'rxjs';
import { ConfirmDialogComponent, ConfirmDialogOptions } from './confirm-dialog.component';

@Injectable({
  providedIn: 'root'
})
export class ConfirmDialogService {
  private readonly dialog = inject(MatDialog);

  // Emits true only when the user confirms; closing the dialog in any other way counts as a refusal.
  confirm(options: ConfirmDialogOptions): Observable<boolean> {
    return this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogOptions, boolean>(ConfirmDialogComponent, {
        data: options,
        width: '440px',
        maxWidth: 'calc(100vw - 32px)',
        panelClass: 'confirm-dialog-panel',
        autoFocus: 'first-tabbable',
        restoreFocus: true,
        ariaLabelledBy: 'confirm-dialog-title',
        ariaDescribedBy: 'confirm-dialog-message'
      })
      .afterClosed()
      .pipe(map(choice => choice === true));
  }
}
