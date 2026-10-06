import { LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeBs from '@angular/common/locales/bs';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { DashboardEvent } from '../../../models/dashboard.model';
import { DashboardNextEventComponent } from './dashboard-next-event.component';

describe('DashboardNextEventComponent', () => {
  let fixture: ComponentFixture<DashboardNextEventComponent>;

  beforeAll(() => registerLocaleData(localeBs));

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([]), { provide: LOCALE_ID, useValue: 'bs' }] });
    fixture = TestBed.createComponent(DashboardNextEventComponent);
  });

  function show(event: DashboardEvent | null): HTMLElement {
    fixture.componentRef.setInput('event', event);
    fixture.detectChanges();
    return fixture.nativeElement;
  }

  function text(page: HTMLElement, selector: string): string {
    return page.querySelector(selector)!.textContent!.replace(/\s+/g, ' ').trim();
  }

  it('shows the next event on a ticket with its date, venue and ticket count', () => {
    const page = show({
      eventId: 'jazz-id',
      name: 'Jazz Night',
      date: '2026-11-08T20:00:00',
      venueName: 'Zetra',
      venueCity: 'Sarajevo',
      tickets: 1
    });

    expect([text(page, '.next__day'), text(page, '.next__month'), text(page, '.next__time')]).toEqual(['8.', '11. 2026.', '20:00']);
    expect(page.querySelector('.next__title a')!.getAttribute('href')).toBe('/event/jazz-id');
    expect(text(page, '.next__venue')).toContain('Zetra, Sarajevo');
    expect(text(page, '.next__count')).toBe('1 ticket');
    expect(page.querySelector('a[href="/tickets"]')).not.toBeNull();
  });

  it('invites to browse events when nothing is coming up', () => {
    const page = show(null);

    expect(text(page, '.next__title')).toBe('Nothing coming up');
    expect(page.querySelector('a[href="/events"]')).not.toBeNull();
  });
});
