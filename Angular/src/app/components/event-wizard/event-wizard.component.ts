import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { CdkStepperModule } from '@angular/cdk/stepper';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MAT_FORM_FIELD_DEFAULT_OPTIONS, MatFormFieldDefaultOptions } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Observable, Subject, catchError, forkJoin, map, of, startWith, switchMap } from 'rxjs';
import { ApiError } from '../../core/models/api-error';
import { ToastService } from '../../core/services/toast.service';
import { EventForEdit, EventFormOptions } from '../../models/event.model';
import { EventService } from '../../services/event.service';
import { DetailsStepComponent } from './details-step/details-step.component';
import { summarize } from './event-summary';
import { WIZARD_STEPS, WizardStep, applyServerErrors, createEventWizardForm, fillFromEvent, recheckDate, toEventInput } from './event-wizard-form';
import { ReviewStepComponent } from './review-step/review-step.component';
import { ScheduleStepComponent } from './schedule-step/schedule-step.component';
import { TicketPreviewComponent } from './ticket-preview/ticket-preview.component';
import { TicketsStepComponent } from './tickets-step/tickets-step.component';
import { WizardStepperComponent } from './wizard-stepper/wizard-stepper.component';

interface WizardData {
  options: EventFormOptions;
  event: EventForEdit | null;
}

type LoadOutcome = { data: WizardData } | { error: string } | null;

@Component({
  selector: 'app-event-wizard',
  standalone: true,
  imports: [
    CdkStepperModule,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    DetailsStepComponent,
    ScheduleStepComponent,
    TicketsStepComponent,
    ReviewStepComponent,
    TicketPreviewComponent,
    WizardStepperComponent
  ],
  templateUrl: './event-wizard.component.html',
  styleUrl: './event-wizard.component.scss',
  providers: [
    {
      provide: MAT_FORM_FIELD_DEFAULT_OPTIONS,
      useValue: { appearance: 'outline', subscriptSizing: 'dynamic' } satisfies MatFormFieldDefaultOptions
    }
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EventWizardComponent {
  private readonly events = inject(EventService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new Subject<void>();
  private readonly stepper = viewChild(WizardStepperComponent);

  protected readonly eventId = inject(ActivatedRoute).snapshot.paramMap.get('id');
  protected readonly reviewStep = WIZARD_STEPS.length;
  protected readonly loaded = signal<LoadOutcome>(null);
  protected readonly saving = signal(false);

  protected readonly data = computed(() => {
    const outcome = this.loaded();
    return outcome && 'data' in outcome ? outcome.data : null;
  });

  protected readonly loadError = computed(() => {
    const outcome = this.loaded();
    return outcome && 'error' in outcome ? outcome.error : null;
  });

  protected readonly form = createEventWizardForm(
    locationId => this.data()?.options.venues.find(venue => venue.publicId === locationId)?.capacity ?? null
  );

  private readonly formChanges = toSignal(this.form.valueChanges);

  protected readonly summary = computed(() => {
    this.formChanges();
    const data = this.data();
    return data ? summarize(this.form, data.options) : null;
  });

  constructor() {
    this.reload$
      .pipe(
        startWith(undefined),
        switchMap(() => this.load()),
        takeUntilDestroyed()
      )
      .subscribe(outcome => this.show(outcome));
  }

  protected retry(): void {
    this.loaded.set(null);
    this.reload$.next();
  }

  protected recheckDate(): void {
    recheckDate(this.form);
  }

  protected goTo(step: WizardStep): void {
    this.stepper()?.go(WIZARD_STEPS.indexOf(step));
  }

  protected save(): void {
    const stepper = this.stepper();
    if (this.saving() || !stepper) return;

    recheckDate(this.form);
    if (this.form.invalid) {
      stepper.go(this.reviewStep);
      return;
    }

    const input = toEventInput(this.form);
    const request: Observable<unknown> = this.eventId
      ? this.events.updateEvent(this.eventId, input)
      : this.events.createEvent(input);

    this.saving.set(true);
    this.form.disable();
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toast.success(this.eventId ? `Changes to "${input.name}" were saved.` : `"${input.name}" was created.`);
        this.router.navigateByUrl('/organizer/events');
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.form.enable();
        this.showSaveError(error);
      }
    });
  }

  private load(): Observable<LoadOutcome> {
    return forkJoin({
      options: this.events.getFormOptions(),
      event: this.eventId ? this.events.getEventForEdit(this.eventId) : of(null)
    }).pipe(
      map(({ options, event }): LoadOutcome => ({ data: { options: withCategoryOf(options, event), event } })),
      catchError((error: unknown) => of<LoadOutcome>({ error: messageOf(error) }))
    );
  }

  private show(outcome: LoadOutcome): void {
    if (outcome && 'data' in outcome && outcome.data.event) {
      fillFromEvent(this.form, outcome.data.event);
    }

    this.loaded.set(outcome);
    this.form.controls.tickets.updateValueAndValidity();
  }

  private showSaveError(error: unknown): void {
    if (!(error instanceof ApiError) || Object.keys(error.fieldErrors).length === 0) {
      this.toast.error(messageOf(error));
      return;
    }

    const { firstStep, unplaced } = applyServerErrors(this.form, error.fieldErrors);
    if (firstStep) {
      this.stepper()!.selectedIndex = WIZARD_STEPS.indexOf(firstStep);
    }
    if (unplaced.length > 0) {
      this.toast.error(unplaced.join(' '));
    }
  }
}

// An edited event keeps its category even when that category is no longer offered for new events.
function withCategoryOf(options: EventFormOptions, event: EventForEdit | null): EventFormOptions {
  if (!event || options.categories.some(category => category.publicId === event.categoryId)) return options;
  return { ...options, categories: [{ publicId: event.categoryId, name: event.categoryName }, ...options.categories] };
}

function messageOf(error: unknown): string {
  return error instanceof ApiError ? error.message : 'Something went wrong. Please try again.';
}
