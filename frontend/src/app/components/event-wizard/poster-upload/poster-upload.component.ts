import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { EventWizardForm } from '../event-wizard-form';
import { refreshOnFormEvents } from '../form-events';
import { PosterDropComponent } from '../poster-drop/poster-drop.component';

export interface PosterUpload {
  eventId: string;
  eventName: string;
  savedMessage: string;
  progress: number;
  error: string | null;
}

@Component({
  selector: 'app-poster-upload',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatProgressBarModule, PosterDropComponent],
  templateUrl: './poster-upload.component.html',
  styleUrl: './poster-upload.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PosterUploadComponent {
  readonly state = input.required<PosterUpload>();
  readonly control = input.required<EventWizardForm['controls']['poster']>();
  readonly image = input.required<string | null>();
  readonly hasCurrent = input.required<boolean>();
  readonly retry = output<void>();
  readonly skip = output<void>();

  constructor() {
    refreshOnFormEvents(this.control);
  }
}
