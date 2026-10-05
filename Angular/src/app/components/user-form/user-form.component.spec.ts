import { Directive, input } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, RouterLink, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApiError } from '../../core/models/api-error';
import { ToastService } from '../../core/services/toast.service';
import { RoleOption, UserAccount } from '../../models/user.model';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';
import { UserFormComponent } from './user-form.component';

@Directive({ selector: '[routerLink]', standalone: true })
class RouterLinkStub {
  readonly routerLink = input<unknown>();
}

describe('UserFormComponent', () => {
  const roles: RoleOption[] = [
    { publicId: 'role-admin', name: 'Admin' },
    { publicId: 'role-organizer', name: 'Organizer' },
    { publicId: 'role-user', name: 'User' }
  ];
  const admin: UserAccount = {
    publicId: 'user-admin',
    firstName: 'Amar',
    lastName: 'Hodzic',
    email: 'admin@ticksi.com',
    phone: '+38761100001',
    roleId: 'role-admin',
    roleName: 'Admin',
    isActive: true,
    registrationDate: '2026-10-04T10:00:00'
  };

  let users: jasmine.SpyObj<UserService>;
  let toast: jasmine.SpyObj<ToastService>;
  let router: jasmine.SpyObj<Router>;
  let fixture: ComponentFixture<UserFormComponent>;

  function open(userId: string | null): HTMLElement {
    TestBed.configureTestingModule({
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap(userId ? { id: userId } : {}) } } },
        { provide: Router, useValue: router },
        { provide: AuthService, useValue: { currentUser: () => ({ publicId: 'user-admin' }) } },
        { provide: UserService, useValue: users },
        { provide: ToastService, useValue: toast }
      ]
    }).overrideComponent(UserFormComponent, { remove: { imports: [RouterLink] }, add: { imports: [RouterLinkStub] } });

    fixture = TestBed.createComponent(UserFormComponent);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  function type(page: HTMLElement, name: string, value: string): void {
    const field = page.querySelector<HTMLInputElement>(`input[formcontrolname="${name}"]`)!;
    field.value = value;
    field.dispatchEvent(new Event('input'));
  }

  function submit(page: HTMLElement): void {
    page.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    fixture.detectChanges();
  }

  function errors(page: HTMLElement): string[] {
    return [...page.querySelectorAll('mat-error')].map(error => error.textContent!.trim());
  }

  beforeEach(() => {
    users = jasmine.createSpyObj<UserService>('UserService', ['getRoles', 'getUser', 'createUser', 'updateUser']);
    users.getRoles.and.returnValue(of(roles));
    users.getUser.and.returnValue(of(admin));
    users.createUser.and.returnValue(of(admin));
    users.updateUser.and.returnValue(of(undefined));
    toast = jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error', 'info']);
    router = jasmine.createSpyObj<Router>('Router', ['navigateByUrl']);
  });

  it('creates an account with the User role preselected and the starting password', () => {
    const page = open(null);
    const checked = [...page.querySelectorAll<HTMLInputElement>('input[type="radio"]')].map(radio => radio.checked);
    expect(checked).toEqual([false, false, true]);
    expect(page.querySelector('.role-card--chosen .role-card__name')!.textContent).toBe('User');

    type(page, 'firstName', ' Lana ');
    type(page, 'lastName', 'Kovac');
    type(page, 'email', 'lana@ticksi.com');
    type(page, 'phone', '+387 61 222 333');
    type(page, 'password', 'Secret1!');
    fixture.detectChanges();
    expect(page.querySelector('.meter')!.getAttribute('data-level')).toBe('very-strong');

    submit(page);

    expect(users.createUser).toHaveBeenCalledOnceWith({
      firstName: 'Lana',
      lastName: 'Kovac',
      email: 'lana@ticksi.com',
      phone: '+387 61 222 333',
      roleId: 'role-user',
      isActive: true,
      password: 'Secret1!'
    });
    expect(toast.success).toHaveBeenCalledWith('Lana Kovac was created.');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/admin/users');
  });

  it('shows every browser error on submit and sends nothing', () => {
    const page = open(null);

    submit(page);

    expect(errors(page)).toEqual([
      'First name is required.',
      'Last name is required.',
      'Email is required.',
      'Phone is required.',
      'Password is required.'
    ]);
    expect(users.createUser).not.toHaveBeenCalled();
  });

  it('keeps the own role and status locked, also after the API refuses a change', () => {
    users.updateUser.and.returnValue(
      throwError(() => new ApiError(400, 'validation', 'Validation failed.', { email: ['An account with this email already exists.'] }))
    );
    const page = open('user-admin');
    const locked = () => [...page.querySelectorAll<HTMLInputElement>('input[type="radio"], button[role="switch"]')].every(e => e.disabled);
    expect(locked()).toBeTrue();
    expect(page.querySelector('input[formcontrolname="password"]')).toBeNull();

    type(page, 'email', 'user@ticksi.com');
    submit(page);

    expect(users.updateUser).toHaveBeenCalledOnceWith('user-admin', jasmine.objectContaining({ email: 'user@ticksi.com', roleId: 'role-admin' }));
    expect(errors(page)).toEqual(['An account with this email already exists.']);
    expect(page.querySelector<HTMLInputElement>('input[formcontrolname="email"]')!.disabled).toBeFalse();
    expect(locked()).toBeTrue();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('shows required errors for whitespace-only fields and sends no create request', () => {
    const page = open(null);
    type(page, 'firstName', 'Lana');
    type(page, 'lastName', 'Kovac');
    type(page, 'email', '   ');
    type(page, 'phone', '         ');
    type(page, 'password', '      ');

    submit(page);

    expect(errors(page)).toEqual(['Email is required.', 'Phone is required.', 'Password is required.']);
    expect(users.createUser).not.toHaveBeenCalled();
  });

  it('saves unrelated changes for an account with an API-accepted email', () => {
    users.getUser.and.returnValue(of({ ...admin, email: 'a..b@example.com' }));
    const page = open('user-admin');
    type(page, 'firstName', 'Amina');

    submit(page);

    expect(users.updateUser).toHaveBeenCalledOnceWith('user-admin', jasmine.objectContaining({
      firstName: 'Amina',
      email: 'a..b@example.com'
    }));
    expect(router.navigateByUrl).toHaveBeenCalledWith('/admin/users');
  });

  it('shows the load failure with a way back', () => {
    users.getUser.and.returnValue(throwError(() => new ApiError(404, 'not_found', 'User not found.')));
    const page = open('missing');

    expect(page.querySelector('.state__title')!.textContent).toContain('This account could not be opened');
    expect(page.querySelector('.state__text')!.textContent).toContain('User not found.');
  });
});
