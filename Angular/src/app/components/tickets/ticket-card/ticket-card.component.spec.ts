import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NEVER } from 'rxjs';
import { Ticket } from '../../../models/ticket.model';
import { TicketService } from '../../../services/ticket.service';
import { TicketCardComponent } from './ticket-card.component';

describe('TicketCardComponent', () => {
  let fixture: ComponentFixture<TicketCardComponent>;

  const ticket: Ticket = {
    publicId: 'ticket-1',
    code: 'AM3PHP8ZLNRU',
    status: 'Valid',
    ticketTypeName: 'VIP',
    eventId: 'event-1',
    eventName: 'Autumn Lights Festival',
    eventDate: '2027-01-06T18:00:00',
    venueName: 'Riverside Summer Stage',
    venueCity: 'Zenica',
    orderId: '8981046b-b826-45de-8592-9bbc5da89eab'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: TicketService, useValue: { getQrCode: () => NEVER } }]
    });
    fixture = TestBed.createComponent(TicketCardComponent);
    fixture.componentRef.setInput('ticket', ticket);
    fixture.detectChanges();
  });

  function text(selector: string): string {
    return (fixture.nativeElement as HTMLElement).querySelector(selector)!.textContent!.trim();
  }

  it('shows the type, status, spaced code and a link to the order', () => {
    expect(text('.ticket__type')).toBe('VIP');
    expect(text('.ticket__status--valid')).toBe('Valid');
    expect(text('.ticket__code')).toBe('AM3P HP8Z LNRU');
    expect(text('.ticket__order')).toBe('Order #8981046B');
    expect((fixture.nativeElement as HTMLElement).querySelector('.ticket__order')!.getAttribute('href'))
      .toBe('/orders/8981046b-b826-45de-8592-9bbc5da89eab');
    expect((fixture.nativeElement as HTMLElement).querySelector('app-ticket-qr')).not.toBeNull();
  });

  it('marks a ticket of a past event', () => {
    expect(fixture.nativeElement.classList).not.toContain('ticket--past');

    fixture.componentRef.setInput('past', true);
    fixture.detectChanges();

    expect(fixture.nativeElement.classList).toContain('ticket--past');
  });
});
