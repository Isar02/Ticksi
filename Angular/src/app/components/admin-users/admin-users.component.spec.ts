import { Directive, input } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { MatSlideToggle, MatSlideToggleChange } from '@angular/material/slide-toggle';
import { ActivatedRoute, ParamMap, Params, Router, RouterLink, convertToParamMap } from '@angular/router';
import { BehaviorSubject, Subject, of, throwError } from 'rxjs';
import { ApiError } from '../../core/models/api-error';
import { ToastService } from '../../core/services/toast.service';
import { RoleOption, UserAccount } from '../../models/user.model';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';
import { PagedResult } from '../../services/event.service';
import { ConfirmDialogOptions } from '../shared/confirm-dialog/confirm-dialog.component';
import { ConfirmDialogService } from '../shared/confirm-dialog/confirm-dialog.service';
import { AdminUsersComponent } from './admin-users.component';

@Directive({ selector: '[routerLink]', standalone: true })
class RouterLinkStub {
  readonly routerLink = input<unknown>();
}

describe('AdminUsersComponent', () => {
  const organizerRole: RoleOption = { publicId: 'role-organizer', name: 'Organizer' };
  const amar: UserAccount = {
    publicId: 'user-amar',
    firstName: 'Amar',
    lastName: 'Hadzic',
    email: 'amar@ticksi.com',
    phone: '+387 62 555 444',
    roleId: 'role-user',
    roleName: 'User',
    isActive: true,
    registrationDate: '2026-09-01T10:00:00'
  };

  let address: BehaviorSubject<ParamMap>;
  let users: jasmine.SpyObj<UserService>;
  let toast: jasmine.SpyObj<ToastService>;
  let answers: boolean[];
  let asked: ConfirmDialogOptions[];
  let component: AdminUsersComponent;
  let fixture: ComponentFixture<AdminUsersComponent>;

  beforeEach(() => {
    address = new BehaviorSubject(convertToParamMap({}));
    answers = [];
    asked = [];
    users = jasmine.createSpyObj<UserService>('UserService', ['getUsers', 'getRoles', 'updateUser', 'setActive', 'deleteUser']);
    users.getUsers.and.returnValue(of({ items: [amar], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 }));
    users.getRoles.and.returnValue(of([organizerRole]));
    users.updateUser.and.returnValue(of(undefined));
    users.setActive.and.returnValue(of(undefined));
    toast = jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error', 'info']);

    TestBed.configureTestingModule({
      providers: [
        { provide: ActivatedRoute, useValue: { queryParamMap: address } },
        { provide: Router, useValue: { navigate: (_: unknown, extras: { queryParams: Params }) => navigate(extras.queryParams) } },
        { provide: AuthService, useValue: { currentUser: () => ({ publicId: 'user-admin' }) } },
        { provide: UserService, useValue: users },
        { provide: ToastService, useValue: toast },
        {
          provide: ConfirmDialogService,
          useValue: {
            confirm: (options: ConfirmDialogOptions) => {
              asked.push(options);
              return of(answers.shift() ?? false);
            }
          }
        }
      ]
    }).overrideComponent(AdminUsersComponent, { remove: { imports: [RouterLink] }, add: { imports: [RouterLinkStub] } });

    fixture = TestBed.createComponent(AdminUsersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  function navigate(queryParams: Params): Promise<boolean> {
    const written = Object.entries(queryParams).filter(([, value]) => value !== null && value !== undefined);
    address.next(convertToParamMap(Object.fromEntries(written.map(([key, value]) => [key, String(value)]))));
    return Promise.resolve(true);
  }

  function toggled(checked: boolean): MatSlideToggleChange {
    const source = { checked } as MatSlideToggle;
    return { source, checked };
  }

  it('keeps a search typed just before a sort click', fakeAsync(() => {
    component['filters'].controls.search.setValue('amar');
    component['sortChanged']({ active: 'email', direction: 'desc' });
    tick(300);

    expect(component['query']()).toEqual(jasmine.objectContaining({ search: 'amar', sortBy: 'email', sortDescending: true, page: 1 }));
    expect(users.getUsers).toHaveBeenCalledWith(jasmine.objectContaining({ search: 'amar', sortBy: 'email' }));
  }));

  it('deactivates only after the confirmation and keeps the toggle on until then', () => {
    const refused = toggled(false);
    component['toggleActive'](amar, refused);

    expect(refused.source.checked).toBeTrue();
    expect(users.setActive).not.toHaveBeenCalled();

    answers = [true];
    component['toggleActive'](amar, toggled(false));

    expect(asked[1]).toEqual(jasmine.objectContaining({ title: 'Deactivate Amar Hadzic?', destructive: true }));
    expect(users.setActive).toHaveBeenCalledOnceWith('user-amar', false);
    expect(asked[1].message).toContain('up to 15 minutes');
    expect(toast.success).toHaveBeenCalledWith('Amar Hadzic was deactivated. Existing access may last up to 15 minutes.');
    expect(users.getUsers).toHaveBeenCalledTimes(2);
  });

  it('activates an inactive account without asking', () => {
    const inactive = { ...amar, isActive: false };
    component['result'].update(page => page && { ...page, items: [inactive] });
    const change = toggled(true);
    component['toggleActive'](inactive, change);

    expect(asked).toEqual([]);
    expect(change.source.checked).toBeFalse();
    expect(users.setActive).toHaveBeenCalledOnceWith('user-amar', true);
  });

  it('changes the role with the rest of the account unchanged after the confirmation', () => {
    answers = [true];
    component['changeRole'](amar, organizerRole);

    expect(asked[0].title).toBe('Make Amar Hadzic an Organizer?');
    expect(users.updateUser).toHaveBeenCalledOnceWith('user-amar', {
      firstName: 'Amar',
      lastName: 'Hadzic',
      email: 'amar@ticksi.com',
      phone: '+387 62 555 444',
      roleId: 'role-organizer',
      isActive: true
    });
  });

  it('offers deactivation when the API refuses to delete an account with history', () => {
    const refusal = 'An account with orders, events or reviews cannot be deleted. Deactivate it instead.';
    users.deleteUser.and.returnValue(throwError(() => new ApiError(409, 'conflict', refusal)));
    answers = [true, true];

    component['delete'](amar);

    expect(asked[1]).toEqual(jasmine.objectContaining({ title: 'This account cannot be deleted', message: refusal, confirmText: 'Deactivate' }));
    expect(users.setActive).toHaveBeenCalledOnceWith('user-amar', false);
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('shows a refused change in a toast and frees the row', () => {
    users.setActive.and.returnValue(throwError(() => new ApiError(403, 'forbidden', 'You cannot deactivate your own account.')));
    answers = [true];

    component['toggleActive'](amar, toggled(false));

    expect(toast.error).toHaveBeenCalledWith('You cannot deactivate your own account.');
    expect(component['actionsDisabled'](amar)).toBeFalse();
  });

  it('recognises the signed-in administrator by the public id', () => {
    const own = { ...amar, publicId: 'user-admin' };
    expect(component['isSelf'](own)).toBeTrue();
    expect(component['isSelf'](amar)).toBeFalse();
    component['result'].update(page => page && { ...page, items: [own, amar], totalCount: 2 });
    fixture.detectChanges();
    const row: HTMLElement = fixture.nativeElement.querySelector('tr.is-self');
    expect(row.querySelector('.role--button')).toBeNull();
    expect(row.querySelector<HTMLButtonElement>('mat-slide-toggle button')!.disabled).toBeTrue();
    expect(row.querySelector<HTMLButtonElement>('.action--delete')!.disabled).toBeTrue();
  });

  it('blocks stale row actions during reload and uses the refreshed state afterward', () => {
    const refresh = new Subject<PagedResult<UserAccount>>();
    users.getUsers.and.returnValue(refresh);
    users.updateUser.and.returnValue(new Subject<void>());
    answers = [true];

    component['toggleActive'](amar, toggled(false));
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector<HTMLButtonElement>('.role--button')!.disabled).toBeTrue();
    expect(element.querySelector<HTMLButtonElement>('mat-slide-toggle button')!.disabled).toBeTrue();
    expect(element.querySelector<HTMLButtonElement>('.action--delete')!.disabled).toBeTrue();
    component['changeRole'](amar, organizerRole);
    expect(users.updateUser).not.toHaveBeenCalled();

    refresh.next({ items: [{ ...amar, isActive: false }], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 });
    fixture.detectChanges();
    answers = [true];
    component['changeRole'](amar, organizerRole);

    expect(users.updateUser).toHaveBeenCalledWith('user-amar', jasmine.objectContaining({ isActive: false }));
  });

  it('keeps the other row locked when a parallel change succeeds or fails', () => {
    const lana = { ...amar, publicId: 'user-lana', firstName: 'Lana', email: 'lana@ticksi.com' };
    component['result'].update(page => page && { ...page, items: [amar, lana], totalCount: 2 });
    const first = new Subject<void>();
    const second = new Subject<void>();
    users.setActive.and.callFake(id => id === amar.publicId ? first : second);
    answers = [true, true];

    component['toggleActive'](amar, toggled(false));
    component['toggleActive'](lana, toggled(false));
    expect(component['actionsDisabled'](amar)).toBeTrue();
    expect(component['actionsDisabled'](lana)).toBeTrue();

    users.getUsers.and.returnValue(of({ items: [{ ...amar, isActive: false }, lana], page: 1, pageSize: 10, totalCount: 2, totalPages: 1 }));
    first.next();
    first.complete();
    fixture.detectChanges();
    expect(component['actionsDisabled'](amar)).toBeFalse();
    expect(component['actionsDisabled'](lana)).toBeTrue();
    component['toggleActive'](lana, toggled(false));
    expect(users.setActive).toHaveBeenCalledTimes(2);

    second.error(new ApiError(500, 'request_failed', 'Please try again.'));
    expect(component['actionsDisabled'](lana)).toBeFalse();
    expect(toast.error).toHaveBeenCalledWith('Please try again.');
  });

  it('rechecks the account when a confirmation remains open across a reload', () => {
    const confirmation = new Subject<boolean>();
    spyOn(TestBed.inject(ConfirmDialogService), 'confirm').and.returnValue(confirmation);
    users.updateUser.and.returnValue(new Subject<void>());
    component['changeRole'](amar, organizerRole);

    users.getUsers.and.returnValue(of({
      items: [{ ...amar, email: 'new@ticksi.com', isActive: false }], page: 1, pageSize: 10, totalCount: 1, totalPages: 1
    }));
    address.next(convertToParamMap({ search: 'amar' }));
    confirmation.next(true);
    confirmation.complete();

    expect(users.updateUser).toHaveBeenCalledWith('user-amar', jasmine.objectContaining({ email: 'new@ticksi.com', isActive: false }));
  });
});
