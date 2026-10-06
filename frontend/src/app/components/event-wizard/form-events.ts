import { ChangeDetectorRef, Signal, inject } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { AbstractControl } from '@angular/forms';
import { switchMap } from 'rxjs';

// Errors set from outside, such as the API's, reach an OnPush step only through the control's events.
export function refreshOnFormEvents(control: Signal<AbstractControl>): void {
  const changeDetector = inject(ChangeDetectorRef);

  toObservable(control)
    .pipe(
      switchMap(current => current.events),
      takeUntilDestroyed()
    )
    .subscribe(() => changeDetector.markForCheck());
}
