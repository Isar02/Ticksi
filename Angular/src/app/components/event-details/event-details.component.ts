import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { forkJoin } from 'rxjs';

import { EventService } from '../../services/event.service';
import { Event } from '../../models/event.model';
import { EventImageGalleryComponent } from './event-image-gallery.component';

@Component({
  selector: 'app-event-details',
  standalone: true,
  imports: [CommonModule, RouterModule, EventImageGalleryComponent],
  templateUrl: './event-details.component.html',
  styleUrl: './event-details.component.scss',
})
export class EventDetailsComponent implements OnInit {
  eventId!: string;

  event?: Event;
  images: string[] = [];

  loading = true;
  error?: string;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    public eventService: EventService
  ) {}

  ngOnInit(): void {
    this.eventId = this.route.snapshot.paramMap.get('id')!;

    this.loading = true;
    this.error = undefined;

    forkJoin({
      event: this.eventService.getEventById(this.eventId),
      images: this.eventService.getEventImages(this.eventId),
    }).subscribe({
      next: ({ event, images }) => {
        this.event = event;
        this.images = images ?? [];
      },
      error: () => {
        this.error = 'Unable to load event details.';
      },
      complete: () => (this.loading = false),
    });
  }

  goBack(): void {
    this.router.navigate(['/events']);
  }
}
