import { AbstractControl, FormControl, FormGroup, ValidationErrors, Validators } from '@angular/forms';

export type CatalogueFilterForm = ReturnType<typeof catalogueFilterForm>;

export function catalogueFilterForm() {
  return new FormGroup(
    {
      categoryId: new FormControl('', { nonNullable: true }),
      city: new FormControl('', { nonNullable: true }),
      dateFrom: new FormControl('', { nonNullable: true }),
      dateTo: new FormControl('', { nonNullable: true }),
      minPrice: new FormControl<number | null>(null, Validators.min(0)),
      maxPrice: new FormControl<number | null>(null, Validators.min(0))
    },
    { validators: [endNotBeforeStart, maxNotBelowMin] }
  );
}

function endNotBeforeStart(group: AbstractControl): ValidationErrors | null {
  const { dateFrom, dateTo } = group.value as { dateFrom: string; dateTo: string };
  return dateFrom && dateTo && dateTo < dateFrom ? { dateRange: true } : null;
}

function maxNotBelowMin(group: AbstractControl): ValidationErrors | null {
  const { minPrice, maxPrice } = group.value as { minPrice: number | null; maxPrice: number | null };
  return minPrice !== null && maxPrice !== null && maxPrice < minPrice ? { priceRange: true } : null;
}
