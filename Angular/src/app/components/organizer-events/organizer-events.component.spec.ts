import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, Params, Router, convertToParamMap } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { CategoryService } from '../../services/category.service';
import { EventService } from '../../services/event.service';
import { OrganizerEventsComponent } from './organizer-events.component';

describe('OrganizerEventsComponent', () => {
  let address: BehaviorSubject<ParamMap>;
  let component: OrganizerEventsComponent;

  beforeEach(() => {
    address = new BehaviorSubject(convertToParamMap({}));
    const emptyPage = { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 };

    TestBed.configureTestingModule({
      providers: [
        { provide: ActivatedRoute, useValue: { queryParamMap: address } },
        { provide: Router, useValue: { navigate: (_: unknown, extras: { queryParams: Params }) => navigate(extras.queryParams) } },
        { provide: AuthService, useValue: { hasAnyRole: () => false } },
        { provide: CategoryService, useValue: { getPublicCategories: () => of(emptyPage) } },
        {
          provide: EventService,
          useValue: { getManagedEvents: () => of(emptyPage), getFormOptions: () => of({ venues: [], eventTypes: [], organizerCompanies: [] }) }
        }
      ]
    }).overrideComponent(OrganizerEventsComponent, { set: { template: '' } });

    component = TestBed.createComponent(OrganizerEventsComponent).componentInstance;
  });

  function navigate(queryParams: Params): Promise<boolean> {
    const written = Object.entries(queryParams).filter(([, value]) => value !== null && value !== undefined);
    address.next(convertToParamMap(Object.fromEntries(written.map(([key, value]) => [key, String(value)]))));
    return Promise.resolve(true);
  }

  function typeName(name: string): void {
    component['filters'].controls.name.setValue(name);
  }

  it('keeps a name typed just before a sort click', fakeAsync(() => {
    typeName('jazz');
    component['sortChanged']({ active: 'venue', direction: 'desc' });
    tick(300);

    expect(component['query']()).toEqual(jasmine.objectContaining({ name: 'jazz', sortBy: 'venue', sortDescending: true, page: 1 }));
  }));

  it('keeps a name typed just before a page click and stays on that page', fakeAsync(() => {
    typeName('jazz');
    component['pageChanged']({ pageIndex: 1, pageSize: 10, length: 40 });
    tick(300);

    expect(component['query']()).toEqual(jasmine.objectContaining({ name: 'jazz', page: 2 }));
  }));

  it('applies a typed name after the pause and returns to the first page', fakeAsync(() => {
    address.next(convertToParamMap({ page: '3' }));
    typeName('jazz');
    tick(300);

    expect(component['query']()).toEqual(jasmine.objectContaining({ name: 'jazz', page: 1 }));
  }));
});
