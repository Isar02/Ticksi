import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PosterService } from '../../../services/poster.service';
import { PosterUploadResponse } from '../../../models/poster.model';

@Component({
  selector: 'app-drag-drop-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './drag-drop-upload.component.html',
  styleUrls: ['./drag-drop-upload.component.scss']
})
export class DragDropUploadComponent {
  @Input() allowedTypes: string[] = ['.jpg', '.jpeg', '.png', '.gif', '.webp'];
  @Input() maxSizeBytes: number = 5 * 1024 * 1024; // 5MB default

  @Output() uploadSuccess = new EventEmitter<PosterUploadResponse>();
  @Output() uploadError = new EventEmitter<string>();

  isDragging = false;
  isUploading = false;
  previewUrl: string | null = null;
  selectedFileName: string | null = null;

  constructor(private posterService: PosterService) {}

  /**
   * Handle drag over event - highlight drop zone
   */
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }

  /**
   * Handle drag leave event - remove highlight
   */
  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
  }

  /**
   * Handle drop event - process the file
   */
  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.handleFile(files[0]);
    }
  }

  /**
   * Handle file selection via file input
   */
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleFile(input.files[0]);
    }
  }

  /**
   * Process the selected file
   */
  private handleFile(file: File): void {
    // Create preview
    this.selectedFileName = file.name;
    const reader = new FileReader();
    reader.onload = (e) => {
      this.previewUrl = e.target?.result as string;
    };
    reader.readAsDataURL(file);

    // Upload the file
    this.uploadFile(file);
  }

  /**
   * Upload file to backend
   */
  private uploadFile(file: File): void {
    this.isUploading = true;

    this.posterService.uploadPoster(file).subscribe({
      next: (response) => {
        this.isUploading = false;
        this.uploadSuccess.emit(response);
      },
      error: (error) => {
        this.isUploading = false;
        this.previewUrl = null;
        this.selectedFileName = null;
        this.uploadError.emit(error.message || 'Upload failed');
      }
    });
  }

  /**
   * Trigger file input click
   */
  triggerFileInput(): void {
    const fileInput = document.getElementById('fileInput') as HTMLInputElement;
    fileInput?.click();
  }

  /**
   * Clear current selection and preview
   */
  clearSelection(): void {
    this.previewUrl = null;
    this.selectedFileName = null;
    const fileInput = document.getElementById('fileInput') as HTMLInputElement;
    if (fileInput) {
      fileInput.value = '';
    }
  }
}

