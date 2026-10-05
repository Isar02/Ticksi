import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { EventWizardForm } from '../event-wizard-form';
import { PosterDropComponent } from '../poster-drop/poster-drop.component';

@Component({
  selector: 'app-poster-step',
  standalone: true,
  imports: [PosterDropComponent],
  templateUrl: './poster-step.component.html',
  styleUrl: './poster-step.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PosterStepComponent {
  readonly control = input.required<EventWizardForm['controls']['poster']>();
  readonly image = input.required<string | null>();
  readonly hasCurrent = input.required<boolean>();
}
