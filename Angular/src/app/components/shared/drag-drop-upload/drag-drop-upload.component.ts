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
  selectedFile: File | null = null;

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
    // Validate file before processing
    const validationError = this.validateFile(file);
    if (validationError) {
      this.uploadError.emit(validationError);
      return;
    }

    // Store the file for later upload
    this.selectedFile = file;
    this.selectedFileName = file.name;

    // Create preview
    const reader = new FileReader();
    reader.onload = (e) => {
      this.previewUrl = e.target?.result as string;
    };
    reader.readAsDataURL(file);
  }

  /**
   * Validate file type and size
   */
  private validateFile(file: File): string | null {
    // Check file type
    const fileExtension = '.' + file.name.split('.').pop()?.toLowerCase();
    if (!this.allowedTypes.includes(fileExtension)) {
      return `Invalid file type. Allowed types: ${this.allowedTypes.join(', ')}`;
    }

    // Check file size
    if (file.size > this.maxSizeBytes) {
      const maxSizeMB = (this.maxSizeBytes / (1024 * 1024)).toFixed(2);
      const fileSizeMB = (file.size / (1024 * 1024)).toFixed(2);
      return `File size (${fileSizeMB}MB) exceeds maximum allowed size (${maxSizeMB}MB)`;
    }

    // Check if file is actually an image
    if (!file.type.startsWith('image/')) {
      return 'Selected file is not a valid image';
    }

    return null;
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
        this.selectedFile = null;
        this.uploadError.emit(error.message || 'Upload failed');
      }
    });
  }

  /**
   * Confirm and upload the selected file
   */
  confirmUpload(): void {
    if (this.selectedFile && !this.isUploading) {
      this.uploadFile(this.selectedFile);
    }
  }

  /**
   * Get formatted file size
   */
  getFileSize(): string {
    if (!this.selectedFile) return '';
    
    const bytes = this.selectedFile.size;
    if (bytes < 1024) {
      return `${bytes} B`;
    } else if (bytes < 1024 * 1024) {
      return `${(bytes / 1024).toFixed(1)} KB`;
    } else {
      return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
    }
  }

  /**
   * Get file type/extension
   */
  getFileType(): string {
    if (!this.selectedFile) return '';
    return this.selectedFile.type || 'Unknown';
  }

  /**
   * Trigger file input to change the current image
   */
  changeImage(): void {
    if (!this.isUploading) {
      this.triggerFileInput();
    }
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
  clearSelection(event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    this.previewUrl = null;
    this.selectedFileName = null;
    this.selectedFile = null;
    const fileInput = document.getElementById('fileInput') as HTMLInputElement;
    if (fileInput) {
      fileInput.value = '';
    }
  }
}

