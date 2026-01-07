import { Component,ElementRef, HostListener, Input,ViewChild } from '@angular/core';
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
    // ---- ZOOM state ----
  zoomed = false;     // click/tap toggled zoom (locked)
  hovering = false;   // hover zoom (desktop)
  canHover = false;   // hover-capable device

  @ViewChild('zoomWrap', { static: false }) zoomWrap?: ElementRef<HTMLElement>;

  constructor() {
    this.canHover = typeof window !== 'undefined'
      ? window.matchMedia('(hover: hover) and (pointer: fine)').matches
      : false;
  }


  openAt(i: number): void {
    if (!this.images?.length) return;
    this.activeIndex = Math.max(0, Math.min(i, this.images.length - 1));
    this.isOpen = true;
    document.body.style.overflow = 'hidden';
    this.resetZoom();

    
  }

  close(): void {
    this.isOpen = false;
    document.body.style.overflow = '';
    this.resetZoom();

  }

  prev(): void {
    if (!this.images?.length) return;
    this.activeIndex = (this.activeIndex - 1 + this.images.length) % this.images.length;
    this.resetZoom();
  }

  next(): void {
    if (!this.images?.length) return;
    this.activeIndex = (this.activeIndex + 1) % this.images.length;
    this.resetZoom();
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

    onPointerEnter(): void {
    if (!this.canHover) return;
    if (this.zoomed) return;
    this.hovering = true;
  }

  onPointerLeave(): void {
    if (!this.canHover) return;
    if (this.zoomed) return;
    this.hovering = false;
    this.setOriginPercent(50, 50);
  }

  onPointerMove(e: PointerEvent): void {
    if (!this.isOpen) return;

    const shouldTrack = this.zoomed || (this.canHover && this.hovering);
    if (!shouldTrack) return;

    const el = this.zoomWrap?.nativeElement;
    if (!el) return;

    const rect = el.getBoundingClientRect();
    const x = ((e.clientX - rect.left) / rect.width) * 100;
    const y = ((e.clientY - rect.top) / rect.height) * 100;

    this.setOriginPercent(this.clamp(x, 0, 100), this.clamp(y, 0, 100));
  }

  toggleZoom(e: MouseEvent): void {
    const t = e.target as HTMLElement;
    if (t?.closest('button')) return; // ne zoomaj kad klikneš nav/close dugmad

    this.zoomed = !this.zoomed;

    if (!this.zoomed) {
      this.setOriginPercent(50, 50);
      if (this.canHover) this.hovering = false;
    } else {
      this.hovering = false;
    }
  }

  private resetZoom(): void {
    this.zoomed = false;
    this.hovering = false;
    this.setOriginPercent(50, 50);
  }

  private setOriginPercent(x: number, y: number): void {
    const el = this.zoomWrap?.nativeElement;
    if (!el) return;
    el.style.setProperty('--ox', `${x}%`);
    el.style.setProperty('--oy', `${y}%`);
  }

  private clamp(v: number, min: number, max: number): number {
    return Math.max(min, Math.min(max, v));
  }

}
