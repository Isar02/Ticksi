import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { Router, provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../services/auth.service';
import { BuyTicketsDialogService } from './buy-tickets-dialog.service';

describe('BuyTicketsDialogService', () => {
  const event = { publicId: 'rock', name: 'Sarajevo Rock Night', date: '2026-12-09T20:00:00', locationName: 'Zetra' };

  let signedIn: boolean;
  let opened: unknown[];
  let closed$: Subject<string | undefined>;
  let toasts: string[];
  let router: Router;

  beforeEach(() => {
    signedIn = true;
    opened = [];
    toasts = [];

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { isAuthenticated: () => signedIn } },
        { provide: ToastService, useValue: { info: (message: string) => toasts.push(message) } },
        {
          provide: MatDialog,
          useValue: {
            open: (_: unknown, config: { data: unknown }) => {
              opened.push(config.data);
              closed$ = new Subject<string | undefined>();
              return { afterClosed: () => closed$ };
            }
          }
        }
      ]
    });

    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
    spyOn(router, 'navigateByUrl').and.resolveTo(true);
  });

  it('sends a guest to sign in and back to the same page', () => {
    signedIn = false;
    spyOnProperty(router, 'url').and.returnValue('/events?city=Mostar');

    TestBed.inject(BuyTicketsDialogService).open(event);

    expect(opened).toEqual([]);
    expect(toasts).toEqual(['Sign in to buy tickets.']);
    const target = (router.navigateByUrl as jasmine.Spy).calls.mostRecent().args[0];
    expect(router.serializeUrl(target)).toBe('/auth/login?returnUrl=%2Fevents%3Fcity%3DMostar');
  });

  it('opens the dialog and goes to the order once it is reserved', () => {
    TestBed.inject(BuyTicketsDialogService).open(event);

    expect(opened).toEqual([event]);
    closed$.next('order-1');
    expect(router.navigate).toHaveBeenCalledOnceWith(['/orders', 'order-1']);
  });

  it('stays on the page when the dialog is closed without an order', () => {
    TestBed.inject(BuyTicketsDialogService).open(event);

    closed$.next(undefined);
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
