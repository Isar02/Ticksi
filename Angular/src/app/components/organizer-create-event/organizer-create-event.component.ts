import { Component, Inject, LOCALE_ID } from '@angular/core';
import { CommonModule, formatDate } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToastService } from '../../core/services/toast.service';

type EventType =
  | 'CONCERT'
  | 'SPORT'
  | 'THEATRE'
  | 'WORKSHOP'
  | 'CONFERENCE'
  | 'OTHER';

@Component({
  selector: 'app-organizer-create-event',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './organizer-create-event.component.html',
  styleUrl: './organizer-create-event.component.scss',
})
export class OrganizerCreateEventComponent {
  // ✅ same style as your CategoryFormComponent
  private fb = new FormBuilder();

  eventTypes: { value: EventType; label: string }[] = [
    { value: 'CONCERT', label: 'Concert' },
    { value: 'SPORT', label: 'Sport' },
    { value: 'THEATRE', label: 'Theatre' },
    { value: 'WORKSHOP', label: 'Workshop' },
    { value: 'CONFERENCE', label: 'Conference' },
    { value: 'OTHER', label: 'Other' },
  ];

  imagePreview: string | null = null;

  form = this.fb.group({
    title: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(80)]],
    type: ['CONCERT' as EventType, [Validators.required]],
    date: ['', [Validators.required]],
    time: ['', [Validators.required]],
    venue: ['', [Validators.required, Validators.maxLength(80)]],
    city: ['', [Validators.required, Validators.maxLength(60)]],
    address: [''],
    description: ['', [Validators.maxLength(800)]],
    capacity: [200, [Validators.required, Validators.min(1), Validators.max(200000)]],
    priceFrom: [0, [Validators.min(0), Validators.max(100000)]],
    currency: ['BAM'],
    isPublic: [true],
    salesStart: [''],
    salesEnd: [''],
    // used only for UI, not uploaded anywhere (frontend-only)
    coverImage: [null as File | null],
  });

  constructor(
    private toast: ToastService,
    @Inject(LOCALE_ID) private locale: string
  ) {}

  // convenience getter used in template
  get f() {
    return this.form.controls;
  }

  onCoverPicked(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    this.form.patchValue({ coverImage: file });
    this.form.markAsDirty();

    if (!file) {
      this.imagePreview = null;
      return;
    }

    // safety check (optional)
    if (!file.type.startsWith('image/')) {
      this.imagePreview = null;
      this.form.patchValue({ coverImage: null });
      return;
    }

    const reader = new FileReader();
    reader.onload = () => (this.imagePreview = String(reader.result));
    reader.readAsDataURL(file);
  }

  reset(): void {
    this.form.reset({
      title: '',
      type: 'CONCERT',
      date: '',
      time: '',
      venue: '',
      city: '',
      address: '',
      description: '',
      capacity: 200,
      priceFrom: 0,
      currency: 'BAM',
      isPublic: true,
      salesStart: '',
      salesEnd: '',
      coverImage: null,
    });

    this.imagePreview = null;
    this.form.markAsPristine();
    this.form.markAsUntouched();
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.toast.info('Saving events is not available yet.');
  }

  get dateTimeLabel(): string {
    const d = this.form.value.date;
    const t = this.form.value.time;
    if (!d || !t) return '—';
    return formatDate(`${d}T${t}`, 'short', this.locale);
  }
}
