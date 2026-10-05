import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { CdkStep, CdkStepper } from '@angular/cdk/stepper';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-wizard-stepper',
  standalone: true,
  imports: [NgTemplateOutlet, MatIconModule],
  templateUrl: './wizard-stepper.component.html',
  styleUrl: './wizard-stepper.component.scss',
  providers: [{ provide: CdkStepper, useExisting: WizardStepperComponent }],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WizardStepperComponent extends CdkStepper {
  readonly leaving = output<void>();

  override next(): void {
    this.go(this.selectedIndex + 1);
  }

  // Any step can be opened once every step before it is valid; otherwise the first invalid one opens with its errors.
  go(index: number): void {
    if (index >= this.steps.length) return;
    if (index > this.selectedIndex) this.leaving.emit();

    const blocking = this.steps.toArray().slice(0, index).findIndex(step => step.stepControl?.invalid);
    if (blocking !== -1) {
      this.steps.get(blocking)!.stepControl.markAllAsTouched();
      index = blocking;
    }

    this.selectedIndex = index;
  }

  // A form locked while saving still counts as done.
  protected isDone(step: CdkStep): boolean {
    return step.interacted && !step.stepControl?.invalid;
  }

  protected needsAttention(step: CdkStep): boolean {
    return step.interacted && !!step.stepControl?.invalid && step.stepControl.touched;
  }
}
