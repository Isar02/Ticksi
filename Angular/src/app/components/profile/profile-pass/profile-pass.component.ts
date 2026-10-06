import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { Profile } from '../../../models/profile.model';
import { ROLE_DESCRIPTIONS } from '../../../models/user.model';

@Component({
  selector: 'app-profile-pass',
  standalone: true,
  imports: [DatePipe, MatIconModule],
  templateUrl: './profile-pass.component.html',
  styleUrl: './profile-pass.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProfilePassComponent {
  readonly profile = input.required<Profile>();

  protected readonly initials = computed(() => {
    const { firstName, lastName } = this.profile();
    return `${firstName.charAt(0)}${lastName.charAt(0)}`.toUpperCase();
  });
  protected readonly roleText = computed(() => ROLE_DESCRIPTIONS[this.profile().role] ?? '');
}
