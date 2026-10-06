import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { ApiError } from '../../../core/models/api-error';
import { Profile, ProfileInput } from '../../../models/profile.model';
import { ProfileService } from '../../../services/profile.service';
import { ProfileDetailsComponent } from './profile-details.component';

describe('ProfileDetailsComponent', () => {
  const profile: Profile = {
    firstName: 'Lejla',
    lastName: 'Begić',
    email: 'lejla@ticksi.com',
    phone: '061 123 456',
    role: 'User',
    registrationDate: '2026-10-04T16:41:04Z'
  };
  let fixture: ComponentFixture<ProfileDetailsComponent>;
  let sent: ProfileInput[];
  let response$: Subject<Profile>;
  let saved: Profile[];

  beforeEach(() => {
    sent = [];
    saved = [];
    TestBed.configureTestingModule({
      providers: [
        {
          provide: ProfileService,
          useValue: {
            update: (input: ProfileInput) => {
              sent.push(input);
              response$ = new Subject<Profile>();
              return response$;
            }
          }
        }
      ]
    });

    fixture = TestBed.createComponent(ProfileDetailsComponent);
    fixture.componentRef.setInput('profile', profile);
    fixture.componentInstance.saved.subscribe(value => saved.push(value));
    fixture.detectChanges();
  });

  function page(): HTMLElement {
    return fixture.nativeElement;
  }

  function input(name: string): HTMLInputElement {
    return page().querySelector(`input[formcontrolname="${name}"]`)!;
  }

  function type(name: string, value: string): void {
    input(name).value = value;
    input(name).dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function save(): HTMLButtonElement {
    return page().querySelector('button[type="submit"]')!;
  }

  function submit(): void {
    page().querySelector('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  it('shows the saved details with the email read-only and nothing to save yet', () => {
    expect([input('firstName').value, input('lastName').value, input('phone').value]).toEqual(['Lejla', 'Begić', '061 123 456']);
    expect(page().querySelector<HTMLInputElement>('input[type="email"]')!.readOnly).toBeTrue();
    expect(save().disabled).toBeTrue();
  });

  it('shows field errors without sending an invalid form', () => {
    type('firstName', 'L');
    type('phone', '12');
    submit();

    expect(sent).toEqual([]);
    expect(Array.from(page().querySelectorAll('mat-error')).map(e => e.textContent!.trim())).toEqual([
      'First name must be at least 2 characters.',
      'Please enter a valid phone number.'
    ]);
  });

  it('sends the trimmed details and reports the saved profile', () => {
    type('firstName', ' Amra ');
    submit();

    expect(sent).toEqual([{ firstName: 'Amra', lastName: 'Begić', phone: '061 123 456' }]);
    expect(save().textContent).toContain('Saving');

    const updated = { ...profile, firstName: 'Amra' };
    response$.next(updated);
    fixture.componentRef.setInput('profile', updated);
    fixture.detectChanges();

    expect(saved).toEqual([updated]);
    expect(input('firstName').value).toBe('Amra');
    expect(save().disabled).toBeTrue();
  });

  it('puts a field error from the API on its field', () => {
    type('phone', '+387 61 987 654');
    submit();
    response$.error(new ApiError(400, 'validation_failed', 'Please enter a valid phone number.', {
      phone: ['Please enter a valid phone number.']
    }));
    fixture.detectChanges();

    expect(page().querySelector('mat-error')!.textContent).toContain('Please enter a valid phone number.');
    expect(page().querySelector('.alert')).toBeNull();
    expect(saved).toEqual([]);
  });

  it('discards unsaved changes', () => {
    type('lastName', 'Hadžić');
    expect(page().querySelector('.actions__note')).not.toBeNull();

    page().querySelector<HTMLButtonElement>('.actions button[type="button"]')!.click();
    fixture.detectChanges();

    expect(input('lastName').value).toBe('Begić');
    expect(page().querySelector('.actions__note')).toBeNull();
  });
});
