import { Component, EventEmitter, Input, Output, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PosterService } from '../../../services/poster.service';
import { PosterUploadResponse } from '../../../models/poster.model';
import { HttpEventType } from '@angular/common/http';

@Component({
  selector: 'app-drag-drop-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './drag-drop-upload.component.html',
  styleUrls: ['./drag-drop-upload.component.scss']
})
export class DragDropUploadComponent {
  @Input() allowedTypes: string[] = ['.jpg', '.jpeg', '.png', '.gif', '.webp'];
  @Input() maxSizeBytes: number = 5 * 1024 * 1024;

  @ViewChild('fileInput') fileInputRef!: ElementRef<HTMLInputElement>;

  @Output() uploadSuccess = new EventEmitter<PosterUploadResponse>();
  @Output() uploadError = new EventEmitter<string>();

  isDragging = false;
  isUploading = false;
  uploadProgress = 0;
  private fakeProgressTimer: any;
  previewUrl: string | null = null;
  selectedFileName: string | null = null;
  selectedFile: File | null = null;

  uploadCompleted = false;

  constructor(private posterService: PosterService) {}

  // ✅ IMPORTANT: only open picker when click happens on the drop-zone itself
  onDropZoneClick(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();

    if (this.isUploading) return;
    if (this.previewUrl) return;

    this.openFilePicker();
  }

  private openFilePicker(): void {
    const input = this.fileInputRef?.nativeElement;
    if (!input) return;

    // reset so selecting same file again triggers change
    input.value = '';
    input.click();
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.handleFile(files[0]);
    }
  }

  onFileSelected(event: Event): void {
    event.preventDefault();
    event.stopPropagation();

    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleFile(input.files[0]);
    }
  }

  private handleFile(file: File): void {
    this.uploadProgress = 0;
    this.uploadCompleted = false;

    const validationError = this.validateFile(file);
    if (validationError) {
      this.uploadError.emit(validationError);
      return;
    }

    this.selectedFile = file;
    this.selectedFileName = file.name;

    const reader = new FileReader();
    reader.onload = (e) => {
      this.previewUrl = e.target?.result as string;
    };
    reader.readAsDataURL(file);
  }

  private validateFile(file: File): string | null {
    const ext = '.' + file.name.split('.').pop()?.toLowerCase();

    if (!this.allowedTypes.includes(ext)) {
      return `Invalid file type. Allowed types: ${this.allowedTypes.join(', ')}`;
    }

    if (file.size > this.maxSizeBytes) {
      const maxMB = (this.maxSizeBytes / (1024 * 1024)).toFixed(2);
      const fileMB = (file.size / (1024 * 1024)).toFixed(2);
      return `File size (${fileMB}MB) exceeds maximum allowed size (${maxMB}MB)`;
    }

    if (!file.type.startsWith('image/')) {
      return 'Selected file is not a valid image';
    }

    return null;
  }

  confirmUpload(): void {
    if (this.selectedFile && !this.isUploading) {
      this.uploadFile(this.selectedFile);
    }
  }

  private uploadFile(file: File): void {
  this.isUploading = true;
  this.uploadProgress = 0;
  this.uploadCompleted = false;

  // 1) Fake progress always moves to 90%
  this.fakeProgressTimer = setInterval(() => {
    if (this.uploadProgress < 90) {
      this.uploadProgress += 1;
    }
  }, 80); // 80ms -> ~7s do 90% (slobodno stavi 150 za sporije)

  // 2) Start upload
  this.posterService.uploadPosterWithProgress(file).subscribe({
    next: (event) => {
      // real progress (if browser sends it)
      if (event.type === HttpEventType.UploadProgress && event.total) {
        const real = Math.round((event.loaded / event.total) * 100);
        if (real > this.uploadProgress && real < 95) {
          this.uploadProgress = real;
        }
      }

      // 3) Response arrived
      if (event.type === HttpEventType.Response) {
        // IMPORTANT: keep the loader visible at least 700ms
        setTimeout(() => {
          clearInterval(this.fakeProgressTimer);

          this.uploadProgress = 100;

          // keep 100% visible briefly so user sees it
          setTimeout(() => {
            this.isUploading = false;
            this.uploadCompleted = true;
            this.uploadSuccess.emit(event.body as PosterUploadResponse);
          }, 400);

        }, 700);
      }
    },
    error: (error) => {
      clearInterval(this.fakeProgressTimer);

      this.isUploading = false;
      this.uploadCompleted = false;
      this.uploadProgress = 0;

      this.previewUrl = null;
      this.selectedFileName = null;
      this.selectedFile = null;

      this.uploadError.emit(error.message || 'Upload failed');
    }
  });
}


  changeImage(): void {
    if (this.isUploading) return;
    this.openFilePicker();
  }

  clearSelection(event?: Event): void {
    if (event) event.stopPropagation();

    this.previewUrl = null;
    this.selectedFileName = null;
    this.selectedFile = null;
    this.uploadCompleted = false;
    this.uploadProgress = 0;

    const input = this.fileInputRef?.nativeElement;
    if (input) input.value = '';
  }

  getFileSize(): string {
    if (!this.selectedFile) return '';
    const bytes = this.selectedFile.size;

    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  }

  getFileType(): string {
    if (!this.selectedFile) return '';
    return this.selectedFile.type || 'Unknown';
  }
}
