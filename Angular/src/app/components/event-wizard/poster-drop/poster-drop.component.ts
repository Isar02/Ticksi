import { ChangeDetectionStrategy, Component, ElementRef, input, signal, viewChild } from '@angular/core';
import { FormControl } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { refreshOnFormEvents } from '../form-events';
import { POSTER_ACCEPT, POSTER_RULES, fileSize, posterProblem } from '../poster-file';

@Component({
  selector: 'app-poster-drop',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './poster-drop.component.html',
  styleUrl: './poster-drop.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PosterDropComponent {
  readonly control = input.required<FormControl<File | null>>();
  readonly image = input.required<string | null>();
  readonly hasCurrent = input(false);

  protected readonly accept = POSTER_ACCEPT;
  protected readonly maxSize = fileSize(POSTER_RULES.maxBytes);
  protected readonly fileSize = fileSize;
  protected readonly problem = signal<string | null>(null);
  protected readonly dragging = signal(false);

  private readonly picker = viewChild.required<ElementRef<HTMLInputElement>>('picker');
  private dragDepth = 0;

  constructor() {
    refreshOnFormEvents(this.control);
  }

  protected choose(): void {
    if (this.control().disabled) return;

    const picker = this.picker().nativeElement;
    picker.value = '';
    picker.click();
  }

  protected picked(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) this.take(file);
  }

  // Counted, because moving over the zone's own children fires a leave for the zone.
  protected dragEnter(event: DragEvent): void {
    event.preventDefault();
    if (this.control().disabled) return;

    this.dragDepth++;
    this.dragging.set(true);
  }

  protected dragOver(event: DragEvent): void {
    event.preventDefault();
    if (event.dataTransfer) event.dataTransfer.dropEffect = this.control().disabled ? 'none' : 'copy';
  }

  protected dragLeave(): void {
    if (this.dragDepth > 0 && --this.dragDepth === 0) this.dragging.set(false);
  }

  protected drop(event: DragEvent): void {
    event.preventDefault();
    this.dragDepth = 0;
    this.dragging.set(false);

    const file = event.dataTransfer?.files[0];
    if (file && !this.control().disabled) this.take(file);
  }

  protected clear(): void {
    this.problem.set(null);
    this.control().setValue(null);
  }

  private take(file: File): void {
    const problem = posterProblem(file);
    this.problem.set(problem);
    if (problem) return;

    this.control().setValue(file);
    this.control().markAsDirty();
  }
}
