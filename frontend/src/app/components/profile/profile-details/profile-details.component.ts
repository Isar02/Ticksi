import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Profile } from '../../../models/profile.model';
import { USER_LIMITS } from '../../../models/user.model';
import { ProfileService } from '../../../services/profile.service';
import { authErrorText, serverFailure } from '../../auth/auth-forms';
import { createDetailsForm, detailsOf, toProfileInput } from '../profile-forms';

@Component({
  selector: 'app-profile-details',
  standalone: true,
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule],
  templateUrl: './profile-details.component.html',
  styleUrl: './profile-details.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProfileDetailsComponent {
  private readonly profiles = inject(ProfileService);
  private readonly destroyRef = inject(DestroyRef);

  readonly profile = input.required<Profile>();
  readonly saved = output<Profile>();

  protected readonly form = createDetailsForm();
  protected readonly limits = USER_LIMITS;
  protected readonly errorText = authErrorText;
  protected readonly submitting = signal(false);
  protected readonly failure = signal<string | null>(null);

  constructor() {
    effect(() => this.form.reset(detailsOf(this.profile())));
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

    const input = toProfileInput(this.form);
    this.submitting.set(true);
    this.form.disable();

    this.profiles.update(input).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: profile => {
        this.form.enable();
        this.submitting.set(false);
        this.saved.emit(profile);
      },
      error: (error: unknown) => {
        this.form.enable();
        this.submitting.set(false);
        this.failure.set(serverFailure(this.form, error));
      }
    });
  }

  protected discard(): void {
    this.failure.set(null);
    this.form.reset(detailsOf(this.profile()));
  }
}
