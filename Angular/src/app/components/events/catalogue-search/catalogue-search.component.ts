import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Subject, catchError, map, of, switchMap, takeUntil, tap, timer } from 'rxjs';
import { SearchService, SearchSuggestionDto } from '../../../services/search.service';
import { SEARCH_MAX_LENGTH } from '../catalogue-query';

const SUGGESTION_ICONS = { event: 'confirmation_number', category: 'sell', location: 'place' } as const;

@Component({
  selector: 'app-catalogue-search',
  standalone: true,
  imports: [ReactiveFormsModule, MatAutocompleteModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule],
  templateUrl: './catalogue-search.component.html',
  styleUrl: './catalogue-search.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CatalogueSearchComponent {
  private readonly search = inject(SearchService);
  private readonly cancelSearch$ = new Subject<void>();

  readonly term = input<string>();
  readonly termChanged = output<string>();
  readonly suggestionPicked = output<SearchSuggestionDto>();

  protected readonly maxLength = SEARCH_MAX_LENGTH;
  protected readonly icons = SUGGESTION_ICONS;
  protected readonly control = new FormControl<string | SearchSuggestionDto>('', {
    nonNullable: true,
    validators: Validators.maxLength(SEARCH_MAX_LENGTH)
  });
  private readonly suggestions = signal<SearchSuggestionDto[]>([]);
  protected readonly groups = computed(() =>
    [
      { label: 'Events', items: this.suggestions().filter(s => s.type === 'event') },
      { label: 'Categories', items: this.suggestions().filter(s => s.type === 'category') },
      { label: 'Venues', items: this.suggestions().filter(s => s.type === 'location') }
    ].filter(group => group.items.length > 0)
  );

  constructor() {
    // The term being typed keeps its trailing space and caret when the address catches up with it.
    effect(() => {
      const term = this.term() ?? '';
      const value = this.control.value;
      if (typeof value !== 'string' || value.trim() !== term) this.resetSearch(term);
    });

    this.control.valueChanges
      .pipe(
        switchMap(value => typeof value !== 'string' ? of([]) : timer(300).pipe(
          map(() => value.trim()),
          tap(term => {
            if (this.control.valid && term !== (this.term() ?? '')) this.termChanged.emit(term);
          }),
          switchMap(term => (term.length < 2 ? of([]) : this.search.getSuggestions(term, 10).pipe(catchError(() => of([]))))),
          takeUntil(this.cancelSearch$)
        )),
        takeUntilDestroyed()
      )
      .subscribe(suggestions => this.suggestions.set(suggestions));
  }

  protected label(value: string | SearchSuggestionDto): string {
    return typeof value === 'string' ? value : value.label;
  }

  protected pick(event: MatAutocompleteSelectedEvent): void {
    const suggestion = event.option.value as SearchSuggestionDto;
    this.resetSearch(suggestion.type === 'event' ? suggestion.label : '');
    this.suggestionPicked.emit(suggestion);
  }

  protected clear(): void {
    this.resetSearch('');
    this.termChanged.emit('');
  }

  private resetSearch(term: string): void {
    this.cancelSearch$.next();
    this.suggestions.set([]);
    this.control.setValue(term, { emitEvent: false });
  }
}
