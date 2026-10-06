import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { ApiError } from '../../../core/models/api-error';
import { PasswordChange } from '../../../models/profile.model';
import { AuthService } from '../../../services/auth.service';
import { ProfilePasswordComponent } from './profile-password.component';

describe('ProfilePasswordComponent', () => {
  let fixture: ComponentFixture<ProfilePasswordComponent>;
  let sent: PasswordChange[];
  let response$: Subject<void>;
  let changed: number;

  beforeEach(() => {
    sent = [];
    changed = 0;
    TestBed.configureTestingModule({
      providers: [
        {
          provide: AuthService,
          useValue: {
            changePassword: (change: PasswordChange) => {
              sent.push(change);
              response$ = new Subject<void>();
              return response$;
            }
          }
        }
      ]
    });

    fixture = TestBed.createComponent(ProfilePasswordComponent);
    fixture.componentInstance.changed.subscribe(() => changed++);
    fixture.detectChanges();
  });

  function page(): HTMLElement {
    return fixture.nativeElement;
  }

  function input(name: string): HTMLInputElement {
    return page().querySelector(`input[formcontrolname="${name}"]`)!;
  }

  function fill(current: string, next: string, confirmation = next): void {
    for (const [name, value] of [['currentPassword', current], ['newPassword', next], ['confirmPassword', confirmation]]) {
      input(name).value = value;
      input(name).dispatchEvent(new Event('input'));
    }
    page().querySelector('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  function errors(): string[] {
    return Array.from(page().querySelectorAll('mat-error')).map(e => e.textContent!.trim());
  }

  it('does not send a short or unconfirmed new password', () => {
    fill('Secret123', '12345', '54321');

    expect(sent).toEqual([]);
    expect(errors()).toEqual(['New password must be at least 6 characters.', 'Passwords do not match.']);
  });

  it('shows the wrong current password on its field', () => {
    fill('Wrong123', 'Changed456');
    response$.error(new ApiError(400, 'validation_failed', 'The current password is incorrect.', {
      currentPassword: ['The current password is incorrect.']
    }));
    fixture.detectChanges();

    expect(errors()).toEqual(['The current password is incorrect.']);
    expect(changed).toBe(0);
  });

  it('sends the change, then clears the form without showing errors', () => {
    fill('Secret123', 'Changed456');
    expect(sent).toEqual([{ currentPassword: 'Secret123', newPassword: 'Changed456' }]);
    expect(page().querySelector('button[type="submit"]')!.textContent).toContain('Changing password');

    response$.next();
    response$.complete();
    fixture.detectChanges();

    expect(changed).toBe(1);
    expect([input('currentPassword').value, input('newPassword').value, input('confirmPassword').value]).toEqual(['', '', '']);
    expect(errors()).toEqual([]);
    expect(page().querySelector('.meter')).toBeNull();
  });

  it('shows a failure that belongs to no field above the form', () => {
    fill('Secret123', 'Changed456');
    response$.error(new ApiError(0, 'network_error', 'Cannot reach the server. Check your connection and try again.'));
    fixture.detectChanges();

    expect(page().querySelector('.alert')!.textContent).toContain('Cannot reach the server.');
    expect(input('currentPassword').value).toBe('Secret123');
  });
});
