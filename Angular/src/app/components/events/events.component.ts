import { Component, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { Subject, of } from 'rxjs';
import {
  debounceTime,
  distinctUntilChanged,
  switchMap,
  catchError,
  tap,
  finalize,
} from 'rxjs/operators';

import { AuthService } from '../../services/auth.service';
import { EventService } from '../../services/event.service';
import { FavoriteService } from '../../services/favorite.service';
import { SearchService, SearchSuggestionDto } from '../../services/search.service';

import { Event } from '../../models/event.model';
import { LoadingSpinnerComponent } from '../shared/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-events',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, LoadingSpinnerComponent],
  templateUrl: './events.component.html',
  styleUrl: './events.component.scss',
})
export class EventsComponent implements OnInit {
  events: Event[] = [];
  currentPage = 1;
  totalPages = 1;
  isLoading = false;
  hasMore = true;

  selectedSort = 'date-desc';

  favoriteIds: Set<string> = new Set();

  // ✅ Unified search input
  searchTerm = '';

  // ✅ Suggestions dropdown (unified: event + category + location)
  suggestions: SearchSuggestionDto[] = [];
  showSuggestions = false;
  isSuggestLoading = false;

  // ✅ Selected filters (set via clicking suggestions)
  selectedCategoryId?: string; // Guid string
  selectedLocationLabel?: string;

  private searchChanged$ = new Subject<string>();

  constructor(
    private eventService: EventService,
    private favoriteService: FavoriteService,
    public authService: AuthService,
    private searchService: SearchService
  ) {}

  ngOnInit(): void {
    this.loadEvents();
    this.loadFavorites();

    // ✅ Suggestion stream (unified)
    this.searchChanged$
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        tap((term) => {
          const t = (term || '').trim();

          // Ako user kuca novo, resetujemo “picked” filtere (jer je sve u jednom searchu)
          if (t.length > 0) {
            this.selectedCategoryId = undefined;
            this.selectedLocationLabel = undefined;
          }

          // Reload results odmah dok user kuca
          this.resetAndReloadEvents();

          if (t.length < 2) {
            this.suggestions = [];
            this.showSuggestions = false;
            this.isSuggestLoading = false;
          }
        }),
        switchMap((term) => {
          const t = (term || '').trim();
          if (t.length < 2) return of([] as SearchSuggestionDto[]);

          this.isSuggestLoading = true;

          return this.searchService.getSuggestions(t, 10).pipe(
            catchError(() => of([] as SearchSuggestionDto[])),
            finalize(() => (this.isSuggestLoading = false))
          );
        })
      )
      .subscribe((list) => {
        this.suggestions = list || [];
        this.showSuggestions = this.suggestions.length > 0;
      });
  }

  // -------------------------
  // Suggestions helpers (for grouped display)
  // -------------------------
  get eventSuggestions(): SearchSuggestionDto[] {
    return (this.suggestions || []).filter((x) => x.type === 'event');
  }

  get categorySuggestions(): SearchSuggestionDto[] {
    return (this.suggestions || []).filter((x) => x.type === 'category');
  }

  get locationSuggestions(): SearchSuggestionDto[] {
    return (this.suggestions || []).filter((x) => x.type === 'location');
  }

  // -------------------------
  // Search handlers (existing UI stays)
  // -------------------------
  onSearchInput(value: string): void {
    this.searchTerm = value;
    this.searchChanged$.next(value);
  }

  onSearchFocus(): void {
    if (this.suggestions.length > 0) this.showSuggestions = true;
  }

  // blur zatvara dropdown, ali ostavi mali delay da click prođe
  onSearchBlur(): void {
    setTimeout(() => (this.showSuggestions = false), 150);
  }

  pickSuggestion(s: SearchSuggestionDto): void {
    this.showSuggestions = false;
    this.suggestions = [];

    // Unutar istog search-a: klik postavlja filtere
    if (s.type === 'category') {
      this.selectedCategoryId = s.publicId;
      this.selectedLocationLabel = undefined;
      this.searchTerm = s.label;
    } else if (s.type === 'location') {
      this.selectedLocationLabel = s.label;
      this.selectedCategoryId = undefined;
      this.searchTerm = s.label;
    } else {
      // event
      this.searchTerm = s.label;
      // event selection ne mora resetovati filtere, ali pošto je 1 input, držimo čisto:
      this.selectedCategoryId = undefined;
      this.selectedLocationLabel = undefined;
    }

    this.resetAndReloadEvents();
  }

  clearSearch(): void {
    // pošto je sve u jednom inputu: clear briše sve
    this.searchTerm = '';
    this.suggestions = [];
    this.showSuggestions = false;
    this.selectedCategoryId = undefined;
    this.selectedLocationLabel = undefined;
    this.resetAndReloadEvents();
  }

  // -------------------------
  // Events loading
  // -------------------------
  private parseSortParams(): { sortBy: string; sortDescending: boolean } {
    const [sortBy, direction] = this.selectedSort.split('-');
    return { sortBy, sortDescending: direction === 'desc' };
  }

  onSortChange(): void {
    this.resetAndReloadEvents();
  }

  private buildEffectiveSearch(): string | undefined {
    const base = (this.searchTerm || '').trim();
    const loc = (this.selectedLocationLabel || '').trim();

    if (!base && !loc) return undefined;
    if (base && !loc) return base;
    if (!base && loc) return loc;

    // izbjegni dupliranje ako je base već location
    if (base.toLowerCase().includes(loc.toLowerCase())) return base;

    return `${base} ${loc}`.trim();
  }

  loadEvents(): void {
    if (this.isLoading || !this.hasMore) return;

    this.isLoading = true;
    const { sortBy, sortDescending } = this.parseSortParams();

    this.eventService
      .getEvents({
        page: this.currentPage,
        pageSize: 10,
        sortBy,
        sortDescending,
        search: this.buildEffectiveSearch(),
        categoryId: this.selectedCategoryId,
      })
      .subscribe({
        next: (response: any) => {
          this.events = [...this.events, ...(response.items || [])];
          this.totalPages = response.totalPages ?? 1;
          this.currentPage++;
          this.hasMore = this.currentPage <= this.totalPages;
          this.isLoading = false;
        },
        error: (error: any) => {
          console.error('Error loading events:', error);
          this.isLoading = false;
        },
      });
  }

  resetAndReloadEvents(): void {
    this.events = [];
    this.currentPage = 1;
    this.hasMore = true;
    this.totalPages = 1;
    this.loadEvents();
  }

  // -------------------------
  // Favorites
  // -------------------------
  loadFavorites(): void {
    if (!this.authService.isAuthenticated()) return;

    this.favoriteService.getUserFavorites().subscribe({
      next: (favoriteIds: string[]) => (this.favoriteIds = new Set(favoriteIds)),
      error: (error: any) => console.error('Error loading favorites:', error),
    });
  }

  isFavorite(eventId: string): boolean {
    return this.favoriteIds.has(eventId);
  }

  toggleFavorite(eventId: string): void {
    if (!this.authService.isAuthenticated()) return;

    const isFavorited = this.isFavorite(eventId);

    if (isFavorited) {
      this.favoriteService.removeFavorite(eventId).subscribe({
        next: () => this.favoriteIds.delete(eventId),
        error: (error: any) => console.error('Error removing favorite:', error),
      });
    } else {
      this.favoriteService.addFavorite(eventId).subscribe({
        next: () => this.favoriteIds.add(eventId),
        error: (error: any) => console.error('Error adding favorite:', error),
      });
    }
  }

  // -------------------------
  // Infinite scroll
  // -------------------------
  @HostListener('window:scroll', [])
  onScroll(): void {
    const scrollPosition = window.innerHeight + window.scrollY;
    const scrollThreshold = document.body.offsetHeight - 300;

    if (scrollPosition >= scrollThreshold) {
      this.loadEvents();
    }
  }
}
