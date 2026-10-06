import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info';

export interface Toast {
  id: number;
  kind: ToastKind;
  message: string;
  durationMs: number;
  closing: boolean;
}

interface ToastTimer {
  handle: ReturnType<typeof setTimeout> | null;
  startedAt: number;
  remainingMs: number;
}

const DURATION_MS: Record<ToastKind, number> = { success: 4000, info: 5000, error: 7000 };
const MAX_VISIBLE = 4;

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private readonly items = signal<Toast[]>([]);
  private readonly timers = new Map<number, ToastTimer>();
  private nextId = 1;

  readonly toasts = this.items.asReadonly();

  success(message: string): void {
    this.show('success', message);
  }

  error(message: string): void {
    this.show('error', message);
  }

  info(message: string): void {
    this.show('info', message);
  }

  close(id: number): void {
    this.clearTimer(id);
    this.items.update(items => items.map(item => (item.id === id ? { ...item, closing: true } : item)));
  }

  remove(id: number): void {
    this.clearTimer(id);
    this.items.update(items => items.filter(item => item.id !== id));
  }

  pause(id: number): void {
    const timer = this.timers.get(id);
    if (timer?.handle) {
      clearTimeout(timer.handle);
      timer.handle = null;
      timer.remainingMs -= Date.now() - timer.startedAt;
    }
  }

  resume(id: number): void {
    const timer = this.timers.get(id);
    if (timer && !timer.handle) {
      this.startTimer(id, timer.remainingMs);
    }
  }

  // Parallel requests failing for the same reason must not stack identical messages.
  private show(kind: ToastKind, message: string): void {
    const visible = this.items().filter(item => !item.closing);
    const duplicate = visible.find(item => item.kind === kind && item.message === message);
    if (duplicate) {
      this.restartTimer(duplicate.id, duplicate.durationMs);
      return;
    }

    if (visible.length >= MAX_VISIBLE) {
      this.close(visible[0].id);
    }

    const toast: Toast = { id: this.nextId++, kind, message, durationMs: DURATION_MS[kind], closing: false };
    this.items.update(items => [...items, toast]);
    this.startTimer(toast.id, toast.durationMs);
  }

  private startTimer(id: number, durationMs: number): void {
    this.timers.set(id, {
      handle: setTimeout(() => this.close(id), Math.max(durationMs, 0)),
      startedAt: Date.now(),
      remainingMs: durationMs
    });
  }

  private restartTimer(id: number, durationMs: number): void {
    const timer = this.timers.get(id);
    if (timer && !timer.handle) {
      timer.remainingMs = durationMs;
      return;
    }

    this.clearTimer(id);
    this.startTimer(id, durationMs);
  }

  private clearTimer(id: number): void {
    const timer = this.timers.get(id);
    if (timer?.handle) {
      clearTimeout(timer.handle);
    }
    this.timers.delete(id);
  }
}
