import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { NavbarComponent } from './navbar.component';

describe('NavbarComponent', () => {
  const user = signal<{ firstName: string } | null>(null);

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: { currentUser: user, hasAnyRole: () => false } }]
    });
  });

  function links(): string[] {
    const fixture = TestBed.createComponent(NavbarComponent);
    fixture.detectChanges();
    return [...fixture.nativeElement.querySelectorAll('.navbar__link')].map((link: HTMLElement) => link.getAttribute('href')!);
  }

  it('shows the dashboard, favorites and tickets links to signed-in visitors only', () => {
    user.set(null);
    expect(links()).not.toContain('/dashboard');
    expect(links()).not.toContain('/favorites');
    expect(links()).not.toContain('/tickets');

    user.set({ firstName: 'Emir' });
    expect(links()).toContain('/dashboard');
    expect(links()).toContain('/favorites');
    expect(links()).toContain('/tickets');
  });

  it('links the signed-in name to the profile page', () => {
    user.set({ firstName: 'Emir' });
    const fixture = TestBed.createComponent(NavbarComponent);
    fixture.detectChanges();

    const profile: HTMLAnchorElement = fixture.nativeElement.querySelector('.navbar__user');
    expect(profile.getAttribute('href')).toBe('/profile');
    expect(profile.textContent).toContain('Emir');
    expect(profile.getAttribute('aria-label')).toBe('Emir, your profile');
  });
});
