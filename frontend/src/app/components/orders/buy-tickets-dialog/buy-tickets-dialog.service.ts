import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { loginUrl } from '../../../core/guards/return-url';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../services/auth.service';
import { BuyTicketsDialogComponent, BuyTicketsDialogData } from './buy-tickets-dialog.component';

@Injectable({
  providedIn: 'root'
})
export class BuyTicketsDialogService {
  private readonly dialog = inject(MatDialog);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  open(event: BuyTicketsDialogData): void {
    if (!this.auth.isAuthenticated()) {
      this.toast.info('Sign in to buy tickets.');
      this.router.navigateByUrl(loginUrl(this.router, this.router.url));
      return;
    }

    this.dialog
      .open<BuyTicketsDialogComponent, BuyTicketsDialogData, string>(BuyTicketsDialogComponent, {
        data: { publicId: event.publicId, name: event.name, date: event.date, locationName: event.locationName },
        width: '520px',
        maxWidth: 'calc(100vw - 32px)',
        panelClass: 'buy-dialog-panel',
        autoFocus: 'first-tabbable',
        restoreFocus: true,
        ariaLabelledBy: 'buy-dialog-title'
      })
      .afterClosed()
      .subscribe(orderId => {
        if (orderId) this.router.navigate(['/orders', orderId]);
      });
  }
}
