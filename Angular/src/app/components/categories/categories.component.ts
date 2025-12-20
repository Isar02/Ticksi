import { Component,OnInit } from '@angular/core';
import { CategoryService } from '../../services/category.service';
import { ReportService } from '../../services/report.service';
import { Category } from '../../models/category.model';
import { CommonModule } from '@angular/common';
import { MatDialog } from '@angular/material/dialog';
import { CategoryFormComponent } from './category-form/category-form.component';
import { PagedResult } from '../../services/category.service';
import { environment} from '../../../environments/environment.development';



@Component({
  selector: 'app-categories',
  imports: [CommonModule],
  standalone: true, 
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.scss'
})
export class CategoriesComponent implements OnInit {
  categories: Category[] = [];
  pagedResult!: PagedResult<Category>;
  downloadingCategories: Set<string> = new Set();

  readonly apiBaseUrl = environment.apiUrl;
  
  constructor(
    private categoryService: CategoryService,
    private reportService: ReportService,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.categoryService.getAll().subscribe(result => {
  this.pagedResult = result;
  this.categories = result.items;
  });

  }

  deleteCategory(publicId: string): void {
    this.categoryService.delete(publicId).subscribe(() => {
      this.loadCategories();
    });
  }

  openForm(category?: Category): void {
  const dialogRef = this.dialog.open(CategoryFormComponent, {
    width: '900px',
    maxWidth: '90vw',
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

 buildPosterUrl(relativeOrAbsolute?: string | null): string {
    if (!relativeOrAbsolute) {
      return '';
    }

    // ako backend nekad vrati pun URL (http...), samo ga vrati
    if (relativeOrAbsolute.startsWith('http')) {
      return relativeOrAbsolute;
    }

    // spoji API bazu i relativni path bez duplih / /
    const base = this.apiBaseUrl.replace(/\/+$/, '');
    const path = relativeOrAbsolute.replace(/^\/+/, '');
    return `${base}/${path}`;
  }

  getPosterUrl(category: Category): string {
  if (!category.posterUrl) {
    // ako nema sliku, stavi neki placeholder
    return 'assets/images/category-placeholder.png';
  }

  // skini /api sa kraja apiUrl-a
  const baseUrl = environment.apiUrl.replace(/\/api\/?$/, '');

  return `${baseUrl}${category.posterUrl}`;
}

  downloadReport(category: Category): void {
    // Prevent duplicate downloads for the same category
    if (this.downloadingCategories.has(category.publicId)) {
      return;
    }

    this.downloadingCategories.add(category.publicId);

    this.reportService.downloadEventsByCategoryReport(category.publicId, category.name)
      .subscribe({
        next: (blob) => {
          const filename = this.reportService.generateFilename(category.name);
          this.reportService.triggerDownload(blob, filename);
          this.downloadingCategories.delete(category.publicId);
          console.log('PDF report downloaded successfully');
        },
        error: (error) => {
          console.error('Error downloading report:', error);
          alert(error.message || 'Failed to download PDF report. Please try again.');
          this.downloadingCategories.delete(category.publicId);
        }
      });
  }

  isDownloading(categoryId: string): boolean {
    return this.downloadingCategories.has(categoryId);
  }


}
