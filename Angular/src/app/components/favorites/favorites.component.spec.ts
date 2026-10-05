import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, Subject, of, throwError } from 'rxjs';
import { ApiError } from '../../core/models/api-error';
import { ToastService } from '../../core/services/toast.service';
import { Event } from '../../models/event.model';
import { FavoriteService } from '../../services/favorite.service';
import { FavoritesComponent } from './favorites.component';

describe('FavoritesComponent', () => {
  let fixture: ComponentFixture<FavoritesComponent>;
  let load: () => Observable<Event[]>;
  let removal: Subject<void>;
  let removed: string[];
  let toasts: string[];

  const event = (name: string): Event =>
    ({ publicId: name, name, date: '2026-12-01T20:00:00', posterUrl: null, lowestPrice: 10, availableTickets: 5, locationName: 'Zetra' }) as Event;

  beforeEach(() => {
    load = () => of([event('Opera'), event('Jazz')]);
    removed = [];
    toasts = [];

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(),
        {
          provide: FavoriteService,
          useValue: {
            getFavoriteEvents: () => load(),
            removeFavorite: (id: string) => {
              removed.push(id);
              removal = new Subject<void>();
              return removal;
            }
          }
        },
        { provide: ToastService, useValue: { success: (m: string) => toasts.push(m), error: (m: string) => toasts.push(m) } }
      ]
    });
  });

  function render(): HTMLElement {
    fixture = TestBed.createComponent(FavoritesComponent);
    fixture.detectChanges();
    return fixture.nativeElement;
  }

  function names(page: HTMLElement): string[] {
    return [...page.querySelectorAll('.card__name')].map(name => name.textContent!.trim());
  }

  it('lists the favorites as cards with a count and a link to each event', () => {
    const page = render();

    expect(names(page)).toEqual(['Opera', 'Jazz']);
    expect(page.querySelector('.head__count')!.textContent!.trim()).toBe('2');
    expect(page.querySelector('.card__link')!.getAttribute('href')).toBe('/event/Opera');
  });

  it('removes a card once the API confirms, and keeps it when the removal fails', () => {
    const page = render();
    const hearts = () => page.querySelectorAll<HTMLButtonElement>('.card__favorite');

    hearts()[0].click();
    fixture.detectChanges();
    expect(hearts()[0].disabled).toBeTrue();
    removal.next();
    removal.complete();
    fixture.detectChanges();

    expect(removed).toEqual(['Opera']);
    expect(names(page)).toEqual(['Jazz']);
    expect(page.querySelector('.head__count')!.textContent!.trim()).toBe('1');

    hearts()[0].click();
    removal.error(new ApiError(404, 'not_found', 'Event not found.', {}));
    fixture.detectChanges();

    expect(names(page)).toEqual(['Jazz']);
    expect(hearts()[0].disabled).toBeFalse();
    expect(toasts.at(-1)).toBe('Event not found.');
  });

  it('explains how to add favorites when there are none and links to the catalogue', () => {
    load = () => of([]);
    const page = render();

    expect(page.querySelector('.state__title')!.textContent).toContain('No favorites yet');
    expect(page.querySelector('.state a')!.getAttribute('href')).toBe('/events');
  });

  it('shows a load failure and loads again on Try again', () => {
    load = () => throwError(() => new ApiError(0, 'network_error', 'Cannot reach the server.', {}));
    const page = render();

    expect(page.querySelector('.state--error')!.textContent).toContain('Cannot reach the server.');

    load = () => of([event('Opera')]);
    page.querySelector<HTMLButtonElement>('.state--error button')!.click();
    fixture.detectChanges();

    expect(names(page)).toEqual(['Opera']);
  });
});
