import { ChangeDetectionStrategy, Component, DestroyRef, inject, output, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormGroupDirective, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { map } from 'rxjs';
import { passwordStrength } from '../../../core/utils/password-strength';
import { USER_LIMITS } from '../../../models/user.model';
import { AuthService } from '../../../services/auth.service';
import { authErrorText, serverFailure } from '../../auth/auth-forms';
import { createPasswordForm, toPasswordChange } from '../profile-forms';

@Component({
  selector: 'app-profile-password',
  standalone: true,
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule],
  templateUrl: './profile-password.component.html',
  styleUrl: './profile-password.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProfilePasswordComponent {
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formDirective = viewChild.required(FormGroupDirective);

  readonly changed = output<void>();

  protected readonly form = createPasswordForm();
  protected readonly limits = USER_LIMITS;
  protected readonly errorText = authErrorText;
  protected readonly submitting = signal(false);
  protected readonly failure = signal<string | null>(null);
  protected readonly passwordsHidden = signal(true);

  protected readonly strength = toSignal(this.form.controls.newPassword.valueChanges.pipe(map(passwordStrength)), {
    initialValue: null
  });

  constructor() {
    this.form.controls.newPassword.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.form.controls.confirmPassword.updateValueAndValidity({ emitEvent: false }));
  }

  protected submit(): void {
    if (this.submitting()) {
      return;
    }

    this.form.markAllAsTouched();
    this.failure.set(null);
    if (this.form.invalid) {
      return;
    }

    const change = toPasswordChange(this.form);
    this.submitting.set(true);
    this.form.disable();

    this.auth.changePassword(change).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.form.enable();
        this.submitting.set(false);
        this.passwordsHidden.set(true);
        this.formDirective().resetForm();
        this.changed.emit();
      },
      error: (error: unknown) => {
        this.form.enable();
        this.submitting.set(false);
        this.failure.set(serverFailure(this.form, error));
      }
    });
  }
}
