import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { EventService } from '../../services/event.service';
import { FavoriteService } from '../../services/favorite.service';
import { Event } from '../../models/event.model';
import { LoadingSpinnerComponent } from '../shared/loading-spinner/loading-spinner.component';

import { Subject, of } from 'rxjs';
import {
  debounceTime,
  distinctUntilChanged,
  switchMap,
  catchError,
  tap,
  finalize,
} from 'rxjs/operators';
import { SearchService, SearchSuggestionDto } from '../../services/search.service';

@Component({
  selector: 'app-events',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, LoadingSpinnerComponent],
  styleUrl: './events.component.scss',
  templateUrl: './events.component.html',
})
export class EventsComponent implements OnInit {
  events: Event[] = [];
  currentPage = 1;
  totalPages = 1;
  isLoading = false;
  hasMore = true;
  selectedSort = 'date-desc';
  favoriteIds: Set<string> = new Set();

  // ✅ search state
  searchTerm = '';
  suggestions: SearchSuggestionDto[] = [];
  showSuggestions = false;
  isSuggestLoading = false;

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

    // ✅ suggestions + auto-search debounce
    this.searchChanged$
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),

        // ✅ Results update instantly while typing
        tap((term) => {
          this.resetAndReloadEvents();

          const t = (term || '').trim();
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

          return this.searchService.getSuggestions(t, 8).pipe(
            catchError(() => of([] as SearchSuggestionDto[])),
            finalize(() => (this.isSuggestLoading = false))
          );
        })
      )
      .subscribe((list) => {
        // za Events ekran nas zanimaju samo event suggestions
        this.suggestions = (list || []).filter((x) => x.type === 'event');
        this.showSuggestions = this.suggestions.length > 0;
      });
  }

  // -------------------------
  // Existing methods
  // -------------------------
  loadFavorites(): void {
    if (!this.authService.isAuthenticated()) return;

    this.favoriteService.getUserFavorites().subscribe({
      next: (favoriteIds) => (this.favoriteIds = new Set(favoriteIds)),
      error: (error) => console.error('Error loading favorites:', error),
    });
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
        search: this.searchTerm?.trim() || undefined,
      })
      .subscribe({
        next: (response) => {
          this.events = [...this.events, ...response.items];
          this.totalPages = response.totalPages;
          this.currentPage++;
          this.hasMore = this.currentPage <= this.totalPages;
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error loading events:', error);
          this.isLoading = false;
        },
      });
  }

  parseSortParams(): { sortBy: string; sortDescending: boolean } {
    const [field, direction] = this.selectedSort.split('-');
    return { sortBy: field, sortDescending: direction === 'desc' };
  }

  onSortChange(): void {
    this.resetAndReloadEvents();
  }

  resetAndReloadEvents(): void {
    this.events = [];
    this.currentPage = 1;
    this.hasMore = true;
    this.totalPages = 1;
    this.loadEvents();
  }

  // -------------------------
  // Search handlers
  // -------------------------
  onSearchInput(value: string): void {
    this.searchTerm = value;
    this.searchChanged$.next(value);
  }

  onSearchFocus(): void {
    if (this.suggestions.length > 0) this.showSuggestions = true;
  }

  // blur zatvara dropdown, ali ostavi mali delay da click na item prođe
  onSearchBlur(): void {
    setTimeout(() => (this.showSuggestions = false), 150);
  }

  pickSuggestion(s: SearchSuggestionDto): void {
    this.searchTerm = s.label;
    this.showSuggestions = false;
    this.suggestions = [];

    // ✅ triggers event search automatically
    this.resetAndReloadEvents();
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.suggestions = [];
    this.showSuggestions = false;
    this.resetAndReloadEvents();
  }

  // -------------------------
  // Scroll + favorites
  // -------------------------
  @HostListener('window:scroll', [])
  onScroll(): void {
    const scrollPosition = window.innerHeight + window.scrollY;
    const scrollThreshold = document.body.offsetHeight - 300;
    if (scrollPosition >= scrollThreshold) this.loadEvents();
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
        error: (error) => console.error('Error removing favorite:', error),
      });
    } else {
      this.favoriteService.addFavorite(eventId).subscribe({
        next: () => this.favoriteIds.add(eventId),
        error: (error) => console.error('Error adding favorite:', error),
      });
    }
  }
}
