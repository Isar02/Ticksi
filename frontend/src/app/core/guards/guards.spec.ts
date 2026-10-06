import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, Routes, convertToParamMap, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { AuthService } from '../../services/auth.service';
import { MANAGER_ROLES, Role } from '../models/role';
import { authGuard } from './auth.guard';
import { guestGuard } from './guest.guard';
import { readReturnUrl } from './return-url';
import { roleGuard } from './role.guard';

@Component({ template: '' })
class PageComponent {}

describe('route guards', () => {
  let role: Role | null;
  let signedIn: boolean;
  let adminLoads: number;

  const routes: Routes = [
    { path: 'auth', canMatch: [guestGuard], children: [{ path: 'login', component: PageComponent }] },
    {
      path: 'admin',
      canMatch: [authGuard, roleGuard(MANAGER_ROLES)],
      loadChildren: () => {
        adminLoads++;
        return [{ path: 'categories', component: PageComponent }];
      }
    },
    { path: '', pathMatch: 'full', component: PageComponent }
  ];

  async function navigate(url: string): Promise<string> {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url);
    return TestBed.inject(Router).url;
  }

  beforeEach(() => {
    role = null;
    signedIn = false;
    adminLoads = 0;

    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: () => signedIn,
            hasAnyRole: (roles: readonly Role[]) => role !== null && roles.includes(role)
          }
        }
      ]
    });
  });

  it('sends a guest to the login page with the requested URL and does not load the area', async () => {
    expect(await navigate('/admin/categories?page=2')).toBe('/auth/login?returnUrl=%2Fadmin%2Fcategories%3Fpage%3D2');
    expect(adminLoads).toBe(0);
  });

  it('sends a signed-in user without a manager role home', async () => {
    signedIn = true;
    role = Role.User;

    expect(await navigate('/admin/categories')).toBe('/');
    expect(adminLoads).toBe(0);
  });

  for (const managerRole of MANAGER_ROLES) {
    it(`opens the area for the ${managerRole} role`, async () => {
      signedIn = true;
      role = managerRole;

      expect(await navigate('/admin/categories')).toBe('/admin/categories');
      expect(adminLoads).toBe(1);
    });
  }

  it('opens the login page for a guest', async () => {
    expect(await navigate('/auth/login')).toBe('/auth/login');
  });

  it('sends a signed-in user away from the login page', async () => {
    signedIn = true;
    role = Role.User;

    expect(await navigate('/auth/login?returnUrl=%2Fevents')).toBe('/');
  });
});

describe('readReturnUrl', () => {
  function read(returnUrl: string | null): string {
    const params = returnUrl === null ? {} : { returnUrl };
    return readReturnUrl({ snapshot: { queryParamMap: convertToParamMap(params) } } as ActivatedRoute);
  }

  it('returns a path inside the application', () => {
    expect(read('/admin/categories?page=2')).toBe('/admin/categories?page=2');
  });

  it('falls back to the home page when the URL is missing or leads elsewhere', () => {
    expect(read(null)).toBe('/');
    expect(read('//example.com')).toBe('/');
    expect(read('https://example.com')).toBe('/');
    expect(read('events')).toBe('/');
  });
});
