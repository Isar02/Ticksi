import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';
import { PosterDropComponent } from './poster-drop.component';

describe('PosterDropComponent', () => {
  let fixture: ComponentFixture<PosterDropComponent>;
  let element: HTMLElement;
  let control: FormControl<File | null>;

  beforeEach(() => {
    control = new FormControl<File | null>(null);
    fixture = TestBed.createComponent(PosterDropComponent);
    fixture.componentRef.setInput('control', control);
    fixture.componentRef.setInput('image', null);
    element = fixture.nativeElement;
    fixture.detectChanges();
  });

  function drop(file: File): void {
    const transfer = new DataTransfer();
    transfer.items.add(file);
    const zone = element.querySelector('.drop')!;
    zone.dispatchEvent(new DragEvent('dragenter', { dataTransfer: transfer, cancelable: true }));
    zone.dispatchEvent(new DragEvent('drop', { dataTransfer: transfer, cancelable: true }));
    fixture.detectChanges();
  }

  it('takes a dropped image', () => {
    const poster = new File([new Uint8Array(64)], 'poster.png', { type: 'image/png' });

    drop(poster);

    expect(control.value).toBe(poster);
    expect(element.querySelector('.problem')).toBeNull();
    expect(element.querySelector('.drop')!.classList).not.toContain('is-dragging');
  });

  it('refuses a file of the wrong type before anything is sent and keeps the chosen one', () => {
    const poster = new File([new Uint8Array(64)], 'poster.png', { type: 'image/png' });
    drop(poster);

    drop(new File(['hello'], 'notes.txt', { type: 'text/plain' }));

    expect(control.value).toBe(poster);
    expect(element.querySelector('.problem')!.textContent).toContain('Choose a JPG, PNG, GIF or WEBP image.');
  });

  it('ignores a drop while the form is locked', () => {
    control.disable();
    fixture.detectChanges();

    drop(new File([new Uint8Array(64)], 'poster.png', { type: 'image/png' }));

    expect(control.value).toBeNull();
  });
});
