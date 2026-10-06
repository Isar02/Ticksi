import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Event } from '../../../models/event.model';
import { EventCardComponent } from './event-card.component';

describe('EventCardComponent', () => {
  let fixture: ComponentFixture<EventCardComponent>;

  const event: Event = {
    publicId: '8d246bf9-195f-44b1-871a-9378162bdbbb',
    name: 'Sevdah Evening',
    description: '',
    date: '2026-12-01T20:00:00',
    contact: '',
    posterUrl: '/images/events/music-1.jpg',
    lowestPrice: 15,
    availableTickets: 120,
    eventCategoryName: 'Music',
    eventCategoryPublicId: '',
    locationName: 'City Theatre Mostar',
    eventTypeName: '',
    organizerCompanyName: ''
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient()] });
    fixture = TestBed.createComponent(EventCardComponent);
  });

  function render(changes: Partial<Event>, favorite: boolean | null = null): HTMLElement {
    fixture.componentRef.setInput('event', { ...event, ...changes });
    fixture.componentRef.setInput('favorite', favorite);
    fixture.detectChanges();
    return fixture.nativeElement;
  }

  it('shows the poster from the API host and links to the event', () => {
    const card = render({});

    expect(card.querySelector('img')!.getAttribute('src')).toMatch(/^https?:\/\/.+\/images\/events\/music-1\.jpg$/);
    expect(card.querySelector('a')!.getAttribute('href')).toBe(`/event/${event.publicId}`);
    expect(card.querySelector('.card__price')!.textContent).toContain('15');
  });

  it('shows a placeholder without a poster and the heart only to signed-in visitors', () => {
    const card = render({ posterUrl: null });

    expect(card.querySelector('img')).toBeNull();
    expect(card.querySelector('.card__placeholder')).not.toBeNull();
    expect(card.querySelector('.card__favorite')).toBeNull();

    render({}, true);
    expect(card.querySelector('.card__favorite')!.getAttribute('aria-pressed')).toBe('true');
  });

  it('tells sold out apart from an event without tickets', () => {
    expect(render({ availableTickets: 0 }).querySelector('.card__price')!.textContent).toContain('Sold out');
    expect(render({ availableTickets: 0, lowestPrice: null }).querySelector('.card__price')!.textContent).toContain('No tickets yet');
    expect(render({ lowestPrice: 0 }).querySelector('.card__price')!.textContent!.trim()).toBe('Free');
  });
});
