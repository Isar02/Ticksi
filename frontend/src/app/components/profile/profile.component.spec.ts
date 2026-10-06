import { LOCALE_ID, signal } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeBs from '@angular/common/locales/bs';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Router, UrlTree, provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { ApiError } from '../../core/models/api-error';
import { ToastService } from '../../core/services/toast.service';
import { Profile } from '../../models/profile.model';
import { AuthService } from '../../services/auth.service';
import { ProfileService } from '../../services/profile.service';
import { ProfileComponent } from './profile.component';
import { ProfileDetailsComponent } from './profile-details/profile-details.component';
import { ProfilePasswordComponent } from './profile-password/profile-password.component';

describe('ProfileComponent', () => {
  const profile: Profile = {
    firstName: 'Lejla',
    lastName: 'Begić',
    email: 'lejla@ticksi.com',
    phone: '061 123 456',
    role: 'Organizer',
    registrationDate: '2026-10-04T16:41:04Z'
  };
  const sessionId = signal<string | null>('lejla-session');
  let fixture: ComponentFixture<ProfileComponent>;
  let profile$: Subject<Profile>;
  let loads: number;
  let renameUser: jasmine.Spy;
  let toast: jasmine.SpyObj<ToastService>;

  beforeAll(() => registerLocaleData(localeBs));

  beforeEach(() => {
    loads = 0;
    sessionId.set('lejla-session');
    renameUser = jasmine.createSpy('renameUser');
    toast = jasmine.createSpyObj<ToastService>('ToastService', ['success']);
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'bs' },
        { provide: ToastService, useValue: toast },
        { provide: AuthService, useValue: { sessionId: () => sessionId(), renameUser } },
        {
          provide: ProfileService,
          useValue: {
            get: () => {
              loads++;
              profile$ = new Subject<Profile>();
              return profile$;
            }
          }
        }
      ]
    });

    fixture = TestBed.createComponent(ProfileComponent);
    fixture.detectChanges();
  });

  function page(): HTMLElement {
    return fixture.nativeElement;
  }

  function child<T>(type: new (...args: never[]) => T): T {
    return fixture.debugElement.query(By.directive(type)).componentInstance as T;
  }

  function show(value: Profile): void {
    profile$.next(value);
    fixture.detectChanges();
  }

  it('shows skeletons, then the pass and both forms', () => {
    expect(page().querySelector('[aria-busy="true"]')).not.toBeNull();

    show(profile);

    const pass = page().querySelector('app-profile-pass')!.textContent!;
    expect(pass).toContain('LB');
    expect(pass).toContain('Lejla Begić');
    expect(pass).toContain('Organizer');
    expect(pass).toContain('Creates events and manages their own.');
    expect(pass).toContain('4. 10. 2026.');
    expect(page().querySelector('app-profile-details')).not.toBeNull();
    expect(page().querySelector('app-profile-password')).not.toBeNull();
  });

  it('shows the failure and loads again on Try again', () => {
    profile$.error(new ApiError(0, 'network_error', 'Cannot reach the server.'));
    fixture.detectChanges();

    expect(page().querySelector('.state--error')!.textContent).toContain('Cannot reach the server.');

    page().querySelector<HTMLButtonElement>('.state button')!.click();
    fixture.detectChanges();
    expect(loads).toBe(2);
  });

  it('shows saved details on the pass, renames the navigation and confirms', () => {
    show(profile);

    child(ProfileDetailsComponent).saved.emit({ ...profile, firstName: 'Amra' });
    fixture.detectChanges();

    expect(page().querySelector('app-profile-pass')!.textContent).toContain('Amra Begić');
    expect(renameUser).toHaveBeenCalledOnceWith('Amra');
    expect(toast.success).toHaveBeenCalledOnceWith('Your details have been saved.');
  });

  it('confirms a password change', () => {
    show(profile);

    child(ProfilePasswordComponent).changed.emit();

    expect(toast.success).toHaveBeenCalledOnceWith('Your password has been changed. Your other devices have been signed out.');
  });

  it('sends a sign-out from another tab to the login page and loads again after a new sign-in', () => {
    const router = TestBed.inject(Router);
    const navigate = spyOn(router, 'navigateByUrl').and.returnValue(Promise.resolve(true));
    show(profile);

    sessionId.set(null);
    fixture.detectChanges();
    const target = navigate.calls.mostRecent().args[0] as string | UrlTree;
    expect(typeof target === 'string' ? target : router.serializeUrl(target)).toBe('/auth/login?returnUrl=%2Fprofile');

    sessionId.set('new-session');
    fixture.detectChanges();
    expect(loads).toBe(2);
  });
});
