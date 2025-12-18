import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { EventService } from '../../services/event.service';
import { Event } from '../../models/event.model';
import { LoadingSpinnerComponent } from '../shared/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-events',
  standalone: true,
  imports: [CommonModule, RouterModule, LoadingSpinnerComponent],
  styleUrl: './events.component.scss',
  templateUrl: './events.component.html',
})
export class EventsComponent implements OnInit {
  events: Event[] = [];
  currentPage = 1;
  totalPages = 1;
  isLoading = false;
  hasMore = true;

  constructor(private eventService: EventService) {}

  ngOnInit(): void {
    this.loadEvents();
  }

  loadEvents(): void {
    if (this.isLoading || !this.hasMore) return;

    this.isLoading = true;

    this.eventService.getEvents({
      page: this.currentPage,
      pageSize: 10
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

  @HostListener('window:scroll', [])
  onScroll(): void {
    const scrollPosition = window.innerHeight + window.scrollY;
    const scrollThreshold = document.body.offsetHeight - 300;

    if (scrollPosition >= scrollThreshold) {
      this.loadEvents();
    }
  }
}