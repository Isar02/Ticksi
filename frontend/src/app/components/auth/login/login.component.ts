import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { readReturnUrl } from '../../../core/guards/return-url';
import { AuthService } from '../../../services/auth.service';
import { authErrorText, createLoginForm, serverFailure, toLoginRequest } from '../auth-forms';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly form = createLoginForm();
  protected readonly errorText = authErrorText;
  protected readonly submitting = signal(false);
  protected readonly failure = signal<string | null>(null);
  protected readonly passwordHidden = signal(true);

  protected submit(): void {
    if (this.submitting()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request = toLoginRequest(this.form);
    this.submitting.set(true);
    this.failure.set(null);
    this.form.disable();

    this.auth.login(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.router.navigateByUrl(readReturnUrl(this.route)),
      error: (error: unknown) => {
        this.form.enable();
        this.submitting.set(false);
        this.failure.set(serverFailure(this.form, error));
      }
    });
  }
}
