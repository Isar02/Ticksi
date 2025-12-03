import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CategoryService, PagedResult } from '../../services/category.service';
import { Category } from '../../models/category.model';
import { FormsModule } from '@angular/forms';
import { debounceTime, Subject } from 'rxjs';

@Component({
  selector: 'app-public-categories',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './public-categories.component.html',
  styleUrl: './public-categories.component.scss'
})
export class PublicCategoriesComponent implements OnInit {

  categories: Category[] = [];
  totalPages = 0;
  totalCount = 0;

  page = 1;
  pageSize = 8;
  search = '';
  filter = '';

  isLoading = false;

  private searchChanged = new Subject<string>();

  constructor(private categoryService: CategoryService) {}

  ngOnInit(): void {

    this.loadCategories();

    this.searchChanged
      .pipe(debounceTime(400))
      .subscribe(() => {
        this.page = 1;
        this.loadCategories();
      });

  }

  loadCategories() {
    this.isLoading = true;

    this.categoryService.getPublicCategories({
      page: this.page,
      pageSize: this.pageSize,
      search: this.search,
      filter: this.filter
    }).subscribe({
      next: res => {
        this.categories = res.items;
        this.totalPages = res.totalPages;
        this.totalCount = res.totalCount;
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }

  onSearch(value: string) {
    this.search = value;
    this.searchChanged.next(value);
  }

  onFilterChange(value: string) {
    this.filter = value;
    this.page = 1;
    this.loadCategories();
  }

  nextPage() {
    if (this.page < this.totalPages) {
      this.page++;
      this.loadCategories();
    }
  }

  prevPage() {
    if (this.page > 1) {
      this.page--;
      this.loadCategories();
    }
  }

}
