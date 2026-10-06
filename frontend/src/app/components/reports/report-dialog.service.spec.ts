import { LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeBs from '@angular/common/locales/bs';
import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { Observable, of } from 'rxjs';
import { ReportPeriod, ReportService } from '../../services/report.service';
import { ReportDialogService } from './report-dialog.service';
import { ReportPeriodDialogData } from './report-period-dialog/report-period-dialog.component';

describe('ReportDialogService', () => {
  let opened: ReportPeriodDialogData[];
  let requests: [string, string, ReportPeriod][];

  beforeAll(() => registerLocaleData(localeBs));

  beforeEach(() => {
    opened = [];
    requests = [];

    TestBed.configureTestingModule({
      providers: [
        { provide: LOCALE_ID, useValue: 'bs' },
        { provide: MatDialog, useValue: { open: (_: unknown, config: { data: ReportPeriodDialogData }) => opened.push(config.data) } },
        {
          provide: ReportService,
          useValue: {
            downloadEventSalesReport: (id: string, range: ReportPeriod) => request('sales', id, range),
            downloadEventsByCategoryReport: (id: string, range: ReportPeriod) => request('events', id, range),
            generateFilename: (kind: string, subject: string) => `${kind}:${subject}`
          }
        }
      ]
    });
  });

  function request(kind: string, id: string, range: ReportPeriod): Observable<Blob> {
    requests.push([kind, id, range]);
    return of(new Blob());
  }

  it('opens the sales report of an event with its date and venue', () => {
    TestBed.inject(ReportDialogService).openEventSalesReport({
      publicId: 'jazz-id',
      name: 'Jazz Night',
      date: '2026-10-08T20:00:00',
      categoryName: 'Music',
      venueName: 'Zetra',
      ticketsSold: 3,
      ticketsTotal: 100
    });

    const [data] = opened;
    expect([data.kind, data.subject, data.detail, data.filename]).toEqual(['Sales report', 'Jazz Night', '8. 10. 2026. · Zetra', 'sales:Jazz Night']);

    data.download({ dateFrom: '2026-10-01', dateTo: '' }).subscribe();
    expect(requests).toEqual([['sales', 'jazz-id', { dateFrom: '2026-10-01', dateTo: '' }]]);
  });

  it('opens the events report of a category without a detail line', () => {
    TestBed.inject(ReportDialogService).openCategoryReport({ publicId: 'music-id', name: 'Music' });

    const [data] = opened;
    expect([data.kind, data.subject, data.detail, data.filename]).toEqual(['Events report', 'Music', undefined, 'events:Music']);

    data.download({ dateFrom: '', dateTo: '' }).subscribe();
    expect(requests).toEqual([['events', 'music-id', { dateFrom: '', dateTo: '' }]]);
  });
});
