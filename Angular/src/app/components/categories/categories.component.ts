import { Component,OnInit } from '@angular/core';
import { CategoryService } from '../../services/category.service';
import { Category } from '../../models/category.model';
import { CommonModule } from '@angular/common';
import { MatDialog } from '@angular/material/dialog';
import { CategoryFormComponent } from './category-form/category-form.component';

@Component({
  selector: 'app-categories',
  imports: [CommonModule],
  standalone: true, 
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.scss'
})
export class CategoriesComponent implements OnInit {
  categories: Category[] = [];

  constructor(private categoryService: CategoryService , private dialog : MatDialog) {}

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.categoryService.getAll().subscribe((data: Category[]) => {
      this.categories = data;
    });
  }

  deleteCategory(publicId: string): void {
    this.categoryService.delete(publicId).subscribe(() => {
      this.loadCategories();
    });
  }

  openForm(category?: Category): void {
  const dialogRef = this.dialog.open(CategoryFormComponent, {
    width: '400px',
    panelClass: 'hide-error-outline-dialog',
    data: { category: category ?? null }
  });

  dialogRef.afterClosed().subscribe(result => {
    console.log('Dialog result:', result);
    if (result) {
      if (category) {
        this.categoryService.update(category.publicId, result).subscribe(() => this.loadCategories());
      } else {
        this.categoryService.create(result).subscribe(() => this.loadCategories());
      }
    }
  });
}

}
