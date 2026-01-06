import { Component, HostListener, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-event-image-gallery',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './event-image-gallery.component.html',
  styleUrl: './event-image-gallery.component.scss',
})
export class EventImageGalleryComponent {
  @Input() images: string[] = [];
  @Input() toUrl: (path: string) => string = (p) => p;

  isOpen = false;
  activeIndex = 0;

  openAt(i: number): void {
    if (!this.images?.length) return;
    this.activeIndex = Math.max(0, Math.min(i, this.images.length - 1));
    this.isOpen = true;
    document.body.style.overflow = 'hidden';
  }

  close(): void {
    this.isOpen = false;
    document.body.style.overflow = '';
  }

  prev(): void {
    if (!this.images?.length) return;
    this.activeIndex = (this.activeIndex - 1 + this.images.length) % this.images.length;
  }

  next(): void {
    if (!this.images?.length) return;
    this.activeIndex = (this.activeIndex + 1) % this.images.length;
  }

  onBackdropClick(e: MouseEvent): void {
    const target = e.target as HTMLElement;
    if (target?.classList?.contains('gallery-modal')) this.close();
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(e: KeyboardEvent): void {
    if (!this.isOpen) return;
    if (e.key === 'Escape') this.close();
    if (e.key === 'ArrowLeft') this.prev();
    if (e.key === 'ArrowRight') this.next();
  }
}
