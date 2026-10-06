import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { ErrorStateMatcher } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Observable, filter, map, of, take, tap } from 'rxjs';
import { readReturnUrl } from '../../../core/guards/return-url';
import { passwordStrength } from '../../../core/utils/password-strength';
import { USER_LIMITS } from '../../../models/user.model';
import { AuthService } from '../../../services/auth.service';
import { authErrorText, createRegisterForm, serverFailure, toRegisterRequest } from '../auth-forms';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly emailAvailability = signal<boolean | null>(null);
  protected readonly form = createRegisterForm(email => {
    this.emailAvailability.set(null);
    return this.auth.isEmailAvailable(email).pipe(tap(available => this.emailAvailability.set(available)));
  });
  protected readonly emailErrorMatcher: ErrorStateMatcher = {
    isErrorState: (control, form) =>
      !!control && control.invalid && (control.hasError('emailTaken') || control.touched || !!form?.submitted)
  };
  protected readonly limits = USER_LIMITS;
  protected readonly errorText = authErrorText;
  protected readonly submitting = signal(false);
  protected readonly failure = signal<string | null>(null);
  protected readonly passwordHidden = signal(true);

  protected readonly emailStatus = toSignal(this.form.controls.email.statusChanges, {
    initialValue: this.form.controls.email.status
  });

  protected readonly strength = toSignal(this.form.controls.password.valueChanges.pipe(map(passwordStrength)), {
    initialValue: null
  });

  constructor() {
    this.form.controls.password.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.form.controls.confirmPassword.updateValueAndValidity({ emitEvent: false }));
  }

  protected submit(): void {
    if (this.submitting()) {
      return;
    }

    this.form.markAllAsTouched();
    this.submitting.set(true);
    this.failure.set(null);

    this.settled()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => (this.form.valid ? this.send() : this.submitting.set(false)));
  }

  // A submit during the email check waits for its answer instead of sending a form that may be refused.
  private settled(): Observable<unknown> {
    return this.form.pending ? this.form.statusChanges.pipe(filter(status => status !== 'PENDING'), take(1)) : of(null);
  }

  private send(): void {
    const request = toRegisterRequest(this.form);
    this.form.disable();

    this.auth.register(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.router.navigateByUrl(readReturnUrl(this.route)),
      error: (error: unknown) => {
        this.form.enable();
        this.submitting.set(false);
        this.failure.set(serverFailure(this.form, error));
      }
    });
  }
}
