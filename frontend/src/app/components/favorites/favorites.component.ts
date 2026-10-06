import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Subject, catchError, finalize, map, of, startWith, switchMap, tap } from 'rxjs';
import { ApiError } from '../../core/models/api-error';
import { ToastService } from '../../core/services/toast.service';
import { Event } from '../../models/event.model';
import { FavoriteService } from '../../services/favorite.service';
import { EventCardComponent } from '../shared/event-card/event-card.component';

type Outcome = { events: Event[] } | { error: string };

@Component({
  selector: 'app-favorites',
  standalone: true,
  imports: [DecimalPipe, RouterLink, MatButtonModule, MatIconModule, EventCardComponent],
  templateUrl: './favorites.component.html',
  styleUrl: './favorites.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FavoritesComponent {
  private readonly favorites = inject(FavoriteService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new Subject<void>();

  protected readonly skeletons = [1, 2, 3];
  protected readonly events = signal<Event[] | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly removing = signal<ReadonlySet<string>>(new Set());

  constructor() {
    this.reload$
      .pipe(
        startWith(undefined),
        tap(() => {
          this.loading.set(true);
          this.error.set(null);
        }),
        switchMap(() =>
          this.favorites.getFavoriteEvents().pipe(
            map((events): Outcome => ({ events })),
            catchError((error: unknown) => of<Outcome>({ error: messageOf(error) }))
          )
        ),
        takeUntilDestroyed()
      )
      .subscribe(outcome => {
        this.loading.set(false);
        if ('error' in outcome) this.error.set(outcome.error);
        else this.events.set(outcome.events);
      });
  }

  protected retry(): void {
    this.reload$.next();
  }

  protected remove(event: Event): void {
    const id = event.publicId;
    if (this.removing().has(id)) return;
    this.removing.update(ids => new Set(ids).add(id));

    this.favorites
      .removeFavorite(id)
      .pipe(
        finalize(() => this.removing.update(ids => without(ids, id))),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.events.update(events => events?.filter(item => item.publicId !== id) ?? null);
          this.toast.success(`"${event.name}" was removed from your favorites.`);
        },
        error: (error: unknown) => this.toast.error(messageOf(error))
      });
  }
}

function without(ids: ReadonlySet<string>, id: string): Set<string> {
  const next = new Set(ids);
  next.delete(id);
  return next;
}

function messageOf(error: unknown): string {
  return error instanceof ApiError ? error.message : 'Something went wrong. Please try again.';
}
