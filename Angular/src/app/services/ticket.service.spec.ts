import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Ticket } from '../models/ticket.model';
import { TicketService } from './ticket.service';

describe('TicketService', () => {
  let service: TicketService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(TicketService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('reads the signed-in buyer\'s tickets', () => {
    let tickets: Ticket[] | undefined;
    service.getMine().subscribe(result => (tickets = result));

    const request = http.expectOne(request => request.url.endsWith('/tickets'));
    expect(request.request.method).toBe('GET');
    request.flush([]);

    expect(tickets).toEqual([]);
  });

  it('downloads a ticket\'s QR code as an image', () => {
    let image: Blob | undefined;
    service.getQrCode('ticket-1').subscribe(result => (image = result));

    const request = http.expectOne(request => request.url.endsWith('/tickets/ticket-1/qr'));
    expect(request.request.responseType).toBe('blob');
    request.flush(new Blob(['png'], { type: 'image/png' }));

    expect(image!.type).toBe('image/png');
  });
});
