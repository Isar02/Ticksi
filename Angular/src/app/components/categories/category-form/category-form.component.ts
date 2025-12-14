import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder,Validators,ReactiveFormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA,MatDialogRef } from '@angular/material/dialog';
import { Category } from '../../../models/category.model';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { Inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { HttpEventType } from '@angular/common/http';
import { PosterService } from '../../../services/poster.service';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { DragDropUploadComponent } from '../../shared/drag-drop-upload/drag-drop-upload.component';
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
    MatProgressBarModule,
    DragDropUploadComponent
  ],
  templateUrl: './category-form.component.html',
  styleUrl: './category-form.component.scss'
})

export class CategoryFormComponent {
  private fb = new FormBuilder;

  categoryForm = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(100),Validators.minLength(2)]],
    description: ['',[Validators.maxLength(500)]]
  });

  constructor(
  fb: FormBuilder,
  private dialogRef: MatDialogRef<CategoryFormComponent>,
  private posterService: PosterService,
  @Inject(MAT_DIALOG_DATA) public data: { category: Category | null }
) {
  if (data.category) {
    this.categoryForm.patchValue({
      name: data.category.name,
      description: data.category.description
    });
  }
}


  saveCategory(): void 
    {
      if (this.categoryForm.valid) 
      {
        const categoryData = this.categoryForm.value;
        this.dialogRef.close(categoryData);
      }
    }

    cancel(): void 
    {
      this.dialogRef.close();
    }

    onFileSelected(event: Event): void {
  const input = event.target as HTMLInputElement;
  if (!input.files || input.files.length === 0) {
    return;
  }

  this.selectedFile = input.files[0];
  this.uploadError = null;

  // Preview
  const reader = new FileReader();
  reader.onload = () => {
    this.previewUrl = reader.result as string;
  };
  reader.readAsDataURL(this.selectedFile);
}

uploadPoster(): void {
  if (!this.selectedFile) {
    return;
  }

  this.uploading = true;
  this.uploadProgress = 0;

  this.posterService.uploadPosterWithProgress(this.selectedFile)
    .subscribe({
      next: event => {
        if (event.type === HttpEventType.UploadProgress && event.total) {
          this.uploadProgress = Math.round((event.loaded / event.total) * 100);
        }

        if (event.type === HttpEventType.Response) {
          this.uploading = false;
          this.uploadedPosterUrl = event.body?.url ?? null;
        }
      },
      error: err => {
        this.uploading = false;
        this.uploadError = err.message ?? 'Upload failed';
      }
    });
}


    selectedFile: File | null = null;
    previewUrl: string | null = null;

    uploadProgress = 0;
    uploading = false;
    uploadError: string | null = null;

    // sačuvani URL sa backenda
    uploadedPosterUrl: string | null = null;

  onPosterUploaded(response: PosterUploadResponse): void {
  this.uploadedPosterUrl = response.url;
  this.uploadError = null;
}

    onPosterUploadError(message: string): void {
    this.uploadError = message;
  }
  
}
