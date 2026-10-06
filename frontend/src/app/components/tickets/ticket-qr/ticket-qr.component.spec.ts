import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { TicketService } from '../../../services/ticket.service';
import { TicketQrComponent } from './ticket-qr.component';

describe('TicketQrComponent', () => {
  let fixture: ComponentFixture<TicketQrComponent>;
  let image$: Subject<Blob>;
  let requested: string[];

  beforeEach(() => {
    requested = [];
    TestBed.configureTestingModule({
      providers: [
        {
          provide: TicketService,
          useValue: {
            getQrCode: (id: string) => {
              requested.push(id);
              image$ = new Subject<Blob>();
              return image$;
            }
          }
        }
      ]
    });

    fixture = TestBed.createComponent(TicketQrComponent);
    fixture.componentRef.setInput('ticketId', 'ticket-1');
    fixture.componentRef.setInput('code', 'AM3PHP8ZLNRU');
    fixture.detectChanges();
  });

  function page(): HTMLElement {
    return fixture.nativeElement;
  }

  it('shows the downloaded code as an image and frees it with the page', () => {
    spyOn(URL, 'revokeObjectURL').and.callThrough();
    expect(page().querySelector('.qr__loading')).not.toBeNull();

    image$.next(new Blob(['png'], { type: 'image/png' }));
    fixture.detectChanges();

    const image = page().querySelector<HTMLImageElement>('.qr__image')!;
    expect(requested).toEqual(['ticket-1']);
    expect(image.src).toMatch(/^blob:/);
    expect(image.alt).toBe('QR code for ticket AM3PHP8ZLNRU');

    fixture.destroy();
    expect(URL.revokeObjectURL).toHaveBeenCalledWith(image.src);
  });

  it('offers another try when the code cannot be downloaded', () => {
    image$.error(new Error('offline'));
    fixture.detectChanges();

    expect(page().querySelector('.qr__failed')!.textContent).toContain('QR code unavailable');
    page().querySelector<HTMLButtonElement>('.qr__retry')!.click();
    fixture.detectChanges();
    expect(page().querySelector('.qr__loading')).not.toBeNull();

    image$.next(new Blob(['png'], { type: 'image/png' }));
    fixture.detectChanges();

    expect(requested).toEqual(['ticket-1', 'ticket-1']);
    expect(page().querySelector('.qr__image')).not.toBeNull();
  });
});
