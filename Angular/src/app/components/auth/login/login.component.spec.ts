import { Directive, input } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, RouterLink, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApiError } from '../../../core/models/api-error';
import { AuthService } from '../../../services/auth.service';
import { LoginComponent } from './login.component';

@Directive({ selector: '[routerLink]', standalone: true })
class RouterLinkStub {
  readonly routerLink = input<unknown>();
}

describe('LoginComponent', () => {
  let auth: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;
  let fixture: ComponentFixture<LoginComponent>;
  let page: HTMLElement;

  function type(name: string, value: string): void {
    const field = page.querySelector<HTMLInputElement>(`input[formcontrolname="${name}"]`)!;
    field.value = value;
    field.dispatchEvent(new Event('input'));
  }

  function submit(): void {
    page.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    fixture.detectChanges();
  }

  function texts(selector: string): string[] {
    return [...page.querySelectorAll(selector)].map(element => element.textContent!.trim());
  }

  beforeEach(() => {
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['login']);
    auth.login.and.returnValue(of(undefined));
    router = jasmine.createSpyObj<Router>('Router', ['navigateByUrl']);

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ returnUrl: '/events/42' }) } } }
      ]
    }).overrideComponent(LoginComponent, { remove: { imports: [RouterLink] }, add: { imports: [RouterLinkStub] } });

    fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    page = fixture.nativeElement as HTMLElement;
  });

  it('signs in with the trimmed email and returns to the page the visitor came from', () => {
    type('email', ' ana@ticksi.com ');
    type('password', 'Secret1');

    submit();

    expect(auth.login).toHaveBeenCalledOnceWith({ email: 'ana@ticksi.com', password: 'Secret1' });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/events/42');
  });

  it('shows wrong credentials above the form and lets the visitor try again', () => {
    auth.login.and.returnValue(throwError(() => new ApiError(401, 'unauthorized', 'Invalid email or password.')));
    type('email', 'ana@ticksi.com');
    type('password', 'Wrong1');

    submit();

    expect(texts('.alert span')).toEqual(['Invalid email or password.']);
    expect(texts('mat-error')).toEqual([]);
    expect(page.querySelector<HTMLInputElement>('input[formcontrolname="email"]')!.disabled).toBeFalse();
    expect(page.querySelector<HTMLButtonElement>('button[type="submit"]')!.disabled).toBeFalse();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('shows the browser errors and sends nothing for an invalid form', () => {
    type('email', 'ana@');
    type('password', '   ');

    submit();

    expect(texts('mat-error')).toEqual(['Invalid email format.', 'Password is required.']);
    expect(auth.login).not.toHaveBeenCalled();
  });

  it('puts API field errors on their fields', () => {
    auth.login.and.returnValue(throwError(() => new ApiError(400, 'validation_failed', 'x', { email: ['Invalid email format.'] })));
    type('email', 'ana@ticksi.com');
    type('password', 'Secret1');

    submit();

    expect(texts('mat-error')).toEqual(['Invalid email format.']);
    expect(texts('.alert span')).toEqual([]);
  });
});
