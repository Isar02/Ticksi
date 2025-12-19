import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { EventService } from '../../services/event.service';
import { FavoriteService } from '../../services/favorite.service';
import { Event } from '../../models/event.model';
import { LoadingSpinnerComponent } from '../shared/loading-spinner/loading-spinner.component';

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

  constructor(
    private eventService: EventService,
    private favoriteService: FavoriteService,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadEvents();
    this.loadFavorites();
  }

  loadFavorites(): void {
    if (!this.authService.isAuthenticated()) {
      return;
    }

    this.favoriteService.getUserFavorites().subscribe({
      next: (favoriteIds) => {
        this.favoriteIds = new Set(favoriteIds);
      },
      error: (error) => {
        console.error('Error loading favorites:', error);
      }
    });
  }

  loadEvents(): void {
    if (this.isLoading || !this.hasMore) return;

    this.isLoading = true;

    const { sortBy, sortDescending } = this.parseSortParams();

    this.eventService.getEvents({
      page: this.currentPage,
      pageSize: 10,
      sortBy,
      sortDescending
    }).subscribe({
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
      }
    });
  }

  parseSortParams(): { sortBy: string; sortDescending: boolean } {
    const [field, direction] = this.selectedSort.split('-');
    return {
      sortBy: field,
      sortDescending: direction === 'desc'
    };
  }

  onSortChange(): void {
    // Reset state
    this.events = [];
    this.currentPage = 1;
    this.hasMore = true;
    this.totalPages = 1;

    // Load events with new sorting
    this.loadEvents();
  }

  @HostListener('window:scroll', [])
  onScroll(): void {
    const scrollPosition = window.innerHeight + window.scrollY;
    const scrollThreshold = document.body.offsetHeight - 300;

    if (scrollPosition >= scrollThreshold) {
      this.loadEvents();
    }
  }

  isFavorite(eventId: string): boolean {
    return this.favoriteIds.has(eventId);
  }

  toggleFavorite(eventId: string, event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();

    if (!this.authService.isAuthenticated()) {
      return;
    }

    const isFavorited = this.isFavorite(eventId);

    if (isFavorited) {
      // Remove from favorites
      this.favoriteService.removeFavorite(eventId).subscribe({
        next: () => {
          this.favoriteIds.delete(eventId);
        },
        error: (error) => {
          console.error('Error removing favorite:', error);
        }
      });
    } else {
      // Add to favorites
      this.favoriteService.addFavorite(eventId).subscribe({
        next: () => {
          this.favoriteIds.add(eventId);
        },
        error: (error) => {
          console.error('Error adding favorite:', error);
        }
      });
    }
  }
}