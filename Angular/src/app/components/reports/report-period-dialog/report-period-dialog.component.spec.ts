import { LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeBs from '@angular/common/locales/bs';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Subject } from 'rxjs';
import { ApiError } from '../../../core/models/api-error';
import { ToastService } from '../../../core/services/toast.service';
import { ReportPeriod, ReportService } from '../../../services/report.service';
import { ReportPeriodDialogComponent, ReportPeriodDialogData } from './report-period-dialog.component';

describe('ReportPeriodDialogComponent', () => {
  let fixture: ComponentFixture<ReportPeriodDialogComponent>;
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
  });

  function open(data: Partial<ReportPeriodDialogData> = {}): void {
    const report: ReportPeriodDialogData = {
      kind: 'Sales report',
      subject: 'Jazz Night',
      message: 'Choose the days of sale the report counts.',
      filename: 'sales-jazz-night-report.pdf',
      download: period => {
        requests.push(period);
        response = new Subject<Blob>();
        return response;
      },
      ...data
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: LOCALE_ID, useValue: 'bs' },
        { provide: MAT_DIALOG_DATA, useValue: report },
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: ReportService, useValue: { triggerDownload: (_: Blob, filename: string) => downloads.push(filename) } },
        { provide: ToastService, useValue: { error: (message: string) => toasts.push(message) } }
      ]
    });

    fixture = TestBed.createComponent(ReportPeriodDialogComponent);
    fixture.detectChanges();
  }

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

  it('names the report and its subject, with the detail only when there is one', () => {
    open({ detail: '8. 10. 2026. · Zetra' });

    expect(dialog().querySelector('.report__eyebrow')!.textContent).toContain('Sales report');
    expect(dialog().querySelector('.report__title')!.textContent).toContain('Jazz Night');
    expect(dialog().querySelector('.report__detail')!.textContent).toContain('8. 10. 2026. · Zetra');
    expect(dialog().querySelector('.ticket__message')!.textContent).toContain('Choose the days of sale the report counts.');

    TestBed.resetTestingModule();
    open();
    expect(dialog().querySelector('.report__detail')).toBeNull();
  });

  it('describes the chosen period in the regional date format', () => {
    open();
    expect(covers()).toBe('All dates');

    setDates('2026-10-01', '');
    expect(covers()).toBe('From 1. 10. 2026.');

    setDates('', '2026-10-31');
    expect(covers()).toBe('Until 31. 10. 2026.');

    setDates('2026-10-01', '2026-10-31');
    expect(covers()).toBe('1. 10. 2026. – 31. 10. 2026.');
  });

  it('asks for the period, then downloads the report and closes', () => {
    open();
    setDates('2026-10-01', '2026-10-31');
    download();

    expect(requests).toEqual([{ dateFrom: '2026-10-01', dateTo: '2026-10-31' }]);
    expect(downloadButton().disabled).toBeTrue();
    expect(dialogRef.disableClose).toBeTrue();

    response.next(new Blob(['%PDF']));
    response.complete();

    expect(downloads).toEqual(['sales-jazz-night-report.pdf']);
    expect(closedWith).toEqual([true]);
  });

  it('shows the chosen dates in the regional format and reads typed ones', async () => {
    open();
    setDates('2026-10-01', '');
    await fixture.whenStable();

    const [from, to] = Array.from(dialog().querySelectorAll<HTMLInputElement>('input'));
    expect(from.value).toBe('1. 10. 2026.');

    to.value = '31. 10. 2026.';
    to.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(fixture.componentInstance['period'].getRawValue()).toEqual({ dateFrom: '2026-10-01', dateTo: '2026-10-31' });
  });

  it('blocks a range that ends before it starts', () => {
    open();
    setDates('2026-10-31', '2026-10-01');

    expect(dialog().querySelector('.report__error')!.textContent).toContain('The end date is before the start date.');
    expect(downloadButton().disabled).toBeTrue();

    fixture.componentInstance.download();
    expect(requests).toEqual([]);
  });

  it('shows a failure as a toast and stays open for another try', () => {
    open();
    download();
    response.error(new ApiError(403, 'forbidden', 'You can only manage your own events.'));
    fixture.detectChanges();

    expect(toasts).toEqual(['You can only manage your own events.']);
    expect(closedWith).toEqual([]);
    expect(downloadButton().disabled).toBeFalse();
    expect(dialogRef.disableClose).toBeFalse();
    expect(fixture.componentInstance['period'].enabled).toBeTrue();
  });
});
