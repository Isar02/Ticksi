import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { Subject } from 'rxjs';
import { SearchService, SearchSuggestionDto } from '../../../services/search.service';
import { CatalogueSearchComponent } from './catalogue-search.component';

describe('CatalogueSearchComponent', () => {
  let fixture: ComponentFixture<CatalogueSearchComponent>;
  let component: CatalogueSearchComponent;
  let terms: string[];
  let picked: SearchSuggestionDto[];
  let responses: Subject<SearchSuggestionDto[]>[];

  beforeEach(() => {
    responses = [];
    TestBed.configureTestingModule({
      providers: [{ provide: SearchService, useValue: { getSuggestions: () => {
        const response = new Subject<SearchSuggestionDto[]>();
        responses.push(response);
        return response;
      } } }]
    }).overrideComponent(CatalogueSearchComponent, { set: { template: '' } });

    fixture = TestBed.createComponent(CatalogueSearchComponent);
    component = fixture.componentInstance;
    terms = [];
    picked = [];
    component.termChanged.subscribe(term => terms.push(term));
    component.suggestionPicked.subscribe(suggestion => picked.push(suggestion));
    fixture.detectChanges();
  });

  function type(value: string): void {
    component['control'].setValue(value);
    tick(300);
  }

  function pick(type: SearchSuggestionDto['type'], label: string): void {
    const value: SearchSuggestionDto = { type, label, publicId: '8d246bf9-195f-44b1-871a-9378162bdbbb', score: 1 };
    component['pick']({ option: { value } } as MatAutocompleteSelectedEvent);
  }

  it('reports the trimmed term after the pause, but not the term already searched', fakeAsync(() => {
    fixture.componentRef.setInput('term', 'jazz');
    fixture.detectChanges();

    type('jazz ');
    type(' rock');

    expect(terms).toEqual(['rock']);
  }));

  it('empties the box for a category and keeps an event name, without reporting a term', fakeAsync(() => {
    type('thea');
    pick('category', 'Theatre');
    expect(component['control'].value).toBe('');

    pick('event', 'Hamlet');
    expect(component['control'].value).toBe('Hamlet');
    expect(terms).toEqual(['thea']);
    expect(picked.map(suggestion => suggestion.label)).toEqual(['Theatre', 'Hamlet']);
  }));

  it('reports the same term again when it is typed after a pick', fakeAsync(() => {
    type('jazz');
    pick('location', 'Zetra');
    type('jazz');

    expect(terms).toEqual(['jazz', 'jazz']);
  }));

  for (const action of ['clear', 'category', 'location', 'event'] as const) {
    it(`cancels pending typing and suggestions on ${action}`, fakeAsync(() => {
      fixture.componentRef.setInput('term', 'jazz');
      fixture.detectChanges();
      const select = () => action === 'clear' ? component['clear']() : pick(action, 'Selected');

      component['control'].setValue('jazzx');
      tick(100);
      select();
      tick(300);

      expect(terms).toEqual(action === 'clear' ? [''] : []);
      expect(responses.length).toBe(0);

      type('rock');
      const response = responses[0];
      select();
      response.next([{ type: 'event', label: 'Stale', publicId: 'stale', score: 1 }]);

      expect(response.observed).toBeFalse();
      expect(component['groups']()).toEqual([]);
    }));
  }

  it('cancels pending typing when the URL restores a different term', fakeAsync(() => {
    component['control'].setValue('pending');
    tick(100);
    fixture.componentRef.setInput('term', 'restored');
    fixture.detectChanges();
    tick(300);

    expect(component['control'].value).toBe('restored');
    expect(terms).toEqual([]);
    expect(responses.length).toBe(0);
  }));

  it('cancels suggestions when the URL restores a different term', fakeAsync(() => {
    type('jazz');
    const response = responses[0];
    fixture.componentRef.setInput('term', 'restored');
    fixture.detectChanges();
    response.next([{ type: 'event', label: 'Stale', publicId: 'stale', score: 1 }]);

    expect(response.observed).toBeFalse();
    expect(component['groups']()).toEqual([]);
  }));
});
