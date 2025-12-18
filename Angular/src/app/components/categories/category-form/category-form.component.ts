import { Component, Inject, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { HttpEventType } from '@angular/common/http';
import { Category } from '../../../models/category.model';
import { CategoryPosterService } from '../../../services/category-poster.service';
import { PosterUploadResponse } from '../../../models/poster.model';

@Component({
  selector: 'app-category-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatDialogModule,
    MatCardModule,
    MatProgressBarModule
  ],
  templateUrl: './category-form.component.html',
  styleUrl: './category-form.component.scss'
})
export class CategoryFormComponent {
  private fb = new FormBuilder();

  @ViewChild('fileInput') fileInputRef!: ElementRef<HTMLInputElement>;

  // dozvoljeni formati i max veličina (5 MB)
  readonly allowedExtensions = ['.jpg', '.jpeg', '.png', '.gif', '.webp'];
  readonly maxFileSizeBytes = 5 * 1024 * 1024;

  categoryForm = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(100), Validators.minLength(2)]],
    description: ['', [Validators.maxLength(500)]],
    posterUrl: ['']
  });

  // upload state
  selectedFile: File | null = null;
  selectedFileName: string | null = null;
  previewUrl: string | null = null;

  uploading = false;
  uploadProgress = 0;
  displayProgress = 0;
  private progressTimer: any = null;

  uploadError: string | null = null;
  uploadedPosterUrl: string | null = null;

  constructor(
    fb: FormBuilder,
    private dialogRef: MatDialogRef<CategoryFormComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { category: Category | null },
    private posterService: CategoryPosterService
  ) {
    if (data.category) {
      this.categoryForm.patchValue({
        name: data.category.name,
        description: data.category.description ?? '',
        posterUrl: data.category.posterUrl ?? ''
      });

      this.uploadedPosterUrl = data.category.posterUrl ?? null;
    }
  }

  // klik na "browse" / dropzonu -> otvara hidden input
  openFileDialog(): void {
    this.uploadError = null;
    if (this.fileInputRef) {
      this.fileInputRef.nativeElement.value = '';
      this.fileInputRef.nativeElement.click();
    }
  }

  // kad se odabere fajl
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      return;
    }

    const file = input.files[0];

    this.uploadError = null;
    this.uploadProgress = 0;
    this.uploading = false;
    this.uploadedPosterUrl = null;

    // ✅ VALIDACIJA: ekstenzija
    const ext = '.' + (file.name.split('.').pop() || '').toLowerCase();
    if (!this.allowedExtensions.includes(ext)) {
      this.uploadError = `Invalid file type. Allowed: ${this.allowedExtensions.join(', ')}`;
      this.resetSelectedFile();
      return;
    }

    // ✅ VALIDACIJA: veličina
    if (file.size > this.maxFileSizeBytes) {
      const sizeMb = (file.size / (1024 * 1024)).toFixed(2);
      this.uploadError = `File is too large (${sizeMb} MB). Maximum allowed size is 5 MB.`;
      this.resetSelectedFile();
      return;
    }

    // dodatni safety: mora biti image/*
    if (!file.type.startsWith('image/')) {
      this.uploadError = 'Selected file is not an image.';
      this.resetSelectedFile();
      return;
    }

    this.selectedFile = file;
    this.selectedFileName = file.name;

    // PREVIEW
    const reader = new FileReader();
    reader.onload = () => {
      this.previewUrl = reader.result as string;
    };
    reader.readAsDataURL(file);
  }

  private resetSelectedFile(): void {
    this.selectedFile = null;
    this.selectedFileName = null;
    this.previewUrl = null;

    if (this.fileInputRef) {
      this.fileInputRef.nativeElement.value = '';
    }
  }

  uploadPoster(): void {
  if (!this.selectedFile || this.uploading) {
    return;
  }

  this.uploading = true;
  this.uploadProgress = 0;
  this.displayProgress = 0;
  this.uploadError = null;

  // ✅ pokreni fake animaciju – lagano diže displayProgress
  if (this.progressTimer) {
    clearInterval(this.progressTimer);
  }

  this.progressTimer = setInterval(() => {
    // neka display polako ide prema "uploadProgress"
    if (this.displayProgress < this.uploadProgress) {
      this.displayProgress += 2; // brzina (povećaj/smanji po želji)
    }

    // sigurnosni plafon
    if (this.displayProgress >= 99) {
      this.displayProgress = 99;
    }
  }, 80); // što je veći broj, to je sporije (ms)
  

  this.posterService.uploadPosterWithProgress(this.selectedFile).subscribe({
    next: (event) => {
      // stvarni progress sa backenda
      if (event.type === HttpEventType.UploadProgress && event.total) {
        const real = Math.round((event.loaded / event.total) * 100);

        // drži bar da ne padne ispod 20% – da ga sigurno vidiš
        this.uploadProgress = Math.max(real, 20);
      }

      if (event.type === HttpEventType.Response) {
        // gotovo: odma digni “pravi” na 100
        this.uploadProgress = 100;

        // kratka animacija da displayProgress dođe do 100
        const finishInterval = setInterval(() => {
          this.displayProgress += 3;
          if (this.displayProgress >= 100) {
            this.displayProgress = 100;
            clearInterval(finishInterval);

            // sada sme da se ugasi main timer
            if (this.progressTimer) {
              clearInterval(this.progressTimer);
              this.progressTimer = null;
            }

            this.uploading = false;
          }
        }, 40);

        const body = event.body as PosterUploadResponse;
        this.uploadedPosterUrl = body.url;

        this.categoryForm.patchValue({
          posterUrl: body.url
        });
      }
    },
    error: (err) => {
      if (this.progressTimer) {
        clearInterval(this.progressTimer);
        this.progressTimer = null;
      }

      this.uploading = false;
      this.uploadProgress = 0;
      this.displayProgress = 0;
      this.uploadError = err.message || 'Error while uploading image.';
    }
  });
}

  ngOnDestroy(): void {
    if (this.progressTimer) {
      clearInterval(this.progressTimer);
    }
  }

  saveCategory(): void {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }

    const formValue = this.categoryForm.value;

    const categoryData = {
      ...formValue,
      posterUrl: this.uploadedPosterUrl ?? formValue.posterUrl ?? null
    };

    this.dialogRef.close(categoryData);
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
