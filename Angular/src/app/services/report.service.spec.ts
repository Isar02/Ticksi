import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ReportService } from './report.service';

describe('ReportService', () => {
  let service: ReportService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(ReportService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends only the dates that are set', () => {
    service.downloadEventsByCategoryReport('music-id', { dateFrom: '2026-10-01', dateTo: '' }).subscribe();
    service.downloadEventsByCategoryReport('music-id', { dateFrom: '', dateTo: '' }).subscribe();

    const [ranged, open] = http.match(request => request.url.endsWith('/reports/events-by-category/music-id'));
    expect(ranged.request.params.keys()).toEqual(['dateFrom']);
    expect(ranged.request.params.get('dateFrom')).toBe('2026-10-01');
    expect(open.request.params.keys()).toEqual([]);
    expect(ranged.request.responseType).toBe('blob');
    ranged.flush(new Blob());
    open.flush(new Blob());
  });
});
