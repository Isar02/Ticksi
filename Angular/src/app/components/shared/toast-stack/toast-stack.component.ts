import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Toast, ToastKind, ToastService } from '../../../core/services/toast.service';

const ICONS: Record<ToastKind, string> = { success: 'check_circle', error: 'error_outline', info: 'info_outline' };

@Component({
  selector: 'app-toast-stack',
  standalone: true,
  templateUrl: './toast-stack.component.html',
  styleUrl: './toast-stack.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ToastStackComponent {
  private readonly toastService = inject(ToastService);

  protected readonly toasts = this.toastService.toasts;
  protected readonly icons = ICONS;

  protected hold(toast: Toast): void {
    this.toastService.pause(toast.id);
  }

  protected release(toast: Toast, element: HTMLElement): void {
    if (!element.matches(':hover, :focus-within')) {
      this.toastService.resume(toast.id);
    }
  }

  protected close(toast: Toast): void {
    this.toastService.close(toast.id);
  }

  protected removeIfClosed(toast: Toast, event: AnimationEvent): void {
    if (toast.closing && event.animationName.endsWith('toast-out')) {
      this.toastService.remove(toast.id);
    }
  }
}
