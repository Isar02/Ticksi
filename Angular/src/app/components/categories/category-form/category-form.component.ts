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
    MatCardModule
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
    @Inject(MAT_DIALOG_DATA) public data: { category: Category | null }
  ) 
  {
    if (data.category) 
    {
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


    
}
