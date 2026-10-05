import { LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeBs from '@angular/common/locales/bs';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Subject } from 'rxjs';
import { ApiError } from '../../../core/models/api-error';
import { ToastService } from '../../../core/services/toast.service';
import { ReportPeriod, ReportService } from '../../../services/report.service';
import { CategoryReportDialogComponent } from './category-report-dialog.component';

describe('CategoryReportDialogComponent', () => {
  let fixture: ComponentFixture<CategoryReportDialogComponent>;
  let requests: ReportPeriod[];
  let response: Subject<Blob>;
  let downloads: string[];
  let toasts: string[];
  let closedWith: boolean[];
  let dialogRef: { disableClose: boolean; close: (result: boolean) => void };

  beforeAll(() => registerLocaleData(localeBs));

  beforeEach(() => {
    requests = [];
    downloads = [];
    toasts = [];
    closedWith = [];
    dialogRef = { disableClose: false, close: result => closedWith.push(result) };

    TestBed.configureTestingModule({
      providers: [
        { provide: LOCALE_ID, useValue: 'bs' },
        { provide: MAT_DIALOG_DATA, useValue: { publicId: 'music-id', name: 'Live Music' } },
        { provide: MatDialogRef, useValue: dialogRef },
        {
          provide: ReportService,
          useValue: {
            downloadEventsByCategoryReport: (_: string, period: ReportPeriod) => {
              requests.push(period);
              response = new Subject<Blob>();
              return response;
            },
            triggerDownload: (_: Blob, filename: string) => downloads.push(filename),
            generateFilename: () => 'events-live-music-report.pdf'
          }
        },
        { provide: ToastService, useValue: { error: (message: string) => toasts.push(message) } }
      ]
    });

    fixture = TestBed.createComponent(CategoryReportDialogComponent);
    fixture.detectChanges();
  });

  function dialog(): HTMLElement {
    return fixture.nativeElement;
  }

  function setDates(dateFrom: string, dateTo: string): void {
    fixture.componentInstance['period'].setValue({ dateFrom, dateTo });
    fixture.detectChanges();
  }

  function downloadButton(): HTMLButtonElement {
    return dialog().querySelector('.ticket__button--confirm')!;
  }

  function covers(): string {
    return dialog().querySelector('.report__covers-value')!.textContent!.trim();
  }

  function download(): void {
    downloadButton().click();
    fixture.detectChanges();
  }

  it('describes the chosen period in the regional date format', () => {
    expect(dialog().querySelector('.report__title')!.textContent).toContain('Live Music');
    expect(covers()).toBe('All dates');

    setDates('2026-10-01', '');
    expect(covers()).toBe('From 1. 10. 2026.');

    setDates('', '2026-10-31');
    expect(covers()).toBe('Until 31. 10. 2026.');

    setDates('2026-10-01', '2026-10-31');
    expect(covers()).toBe('1. 10. 2026. – 31. 10. 2026.');
  });

  it('asks for the period, then downloads the report and closes', () => {
    setDates('2026-10-01', '2026-10-31');
    download();

    expect(requests).toEqual([{ dateFrom: '2026-10-01', dateTo: '2026-10-31' }]);
    expect(downloadButton().disabled).toBeTrue();
    expect(dialogRef.disableClose).toBeTrue();

    response.next(new Blob(['%PDF']));
    response.complete();

    expect(downloads).toEqual(['events-live-music-report.pdf']);
    expect(closedWith).toEqual([true]);
  });

  it('blocks a range that ends before it starts', () => {
    setDates('2026-10-31', '2026-10-01');

    expect(dialog().querySelector('.report__error')!.textContent).toContain('The end date is before the start date.');
    expect(downloadButton().disabled).toBeTrue();

    fixture.componentInstance.download();
    expect(requests).toEqual([]);
  });

  it('shows a failure as a toast and stays open for another try', () => {
    download();
    response.error(new ApiError(404, 'not_found', 'Event category not found.'));
    fixture.detectChanges();

    expect(toasts).toEqual(['Event category not found.']);
    expect(closedWith).toEqual([]);
    expect(downloadButton().disabled).toBeFalse();
    expect(dialogRef.disableClose).toBeFalse();
    expect(fixture.componentInstance['period'].enabled).toBeTrue();
  });
});
