import { Injectable, Provider } from '@angular/core';
import { DateAdapter, MAT_DATE_FORMATS, MatDateFormats } from '@angular/material/core';

const ISO_DATE = /^(\d{4})-(\d{2})-(\d{2})$/;
const TYPED_DATE = /^(\d{1,2})\s*\.\s*(\d{1,2})\s*\.\s*(\d{4})\s*\.?$/;
const INVALID = 'invalid';
const NAMES_LOCALE = 'en-GB';

const DATE_FORMATS: MatDateFormats = {
  parse: { dateInput: 'input' },
  display: { dateInput: 'input', monthYearLabel: 'month-year', dateA11yLabel: 'long', monthYearA11yLabel: 'month-year-long' }
};

// Dates stay yyyy-MM-dd strings, as the forms, the URLs and the API use them; people type and read them as d. M. yyyy.
@Injectable()
export class IsoDateAdapter extends DateAdapter<string> {
  getYear(date: string): number {
    return +date.slice(0, 4);
  }

  getMonth(date: string): number {
    return +date.slice(5, 7) - 1;
  }

  getDate(date: string): number {
    return +date.slice(8, 10);
  }

  getDayOfWeek(date: string): number {
    return toUtc(date).getUTCDay();
  }

  getMonthNames(style: 'long' | 'short' | 'narrow'): string[] {
    const format = new Intl.DateTimeFormat(NAMES_LOCALE, { month: style, timeZone: 'UTC' });
    return Array.from({ length: 12 }, (_, month) => format.format(Date.UTC(2026, month, 1)));
  }

  getDateNames(): string[] {
    return Array.from({ length: 31 }, (_, day) => String(day + 1));
  }

  getDayOfWeekNames(style: 'long' | 'short' | 'narrow'): string[] {
    const format = new Intl.DateTimeFormat(NAMES_LOCALE, { weekday: style, timeZone: 'UTC' });
    return Array.from({ length: 7 }, (_, day) => format.format(Date.UTC(2026, 2, 1 + day)));
  }

  getYearName(date: string): string {
    return String(this.getYear(date));
  }

  getFirstDayOfWeek(): number {
    return 1;
  }

  getNumDaysInMonth(date: string): number {
    return new Date(Date.UTC(this.getYear(date), this.getMonth(date) + 1, 0)).getUTCDate();
  }

  clone(date: string): string {
    return date;
  }

  createDate(year: number, month: number, date: number): string {
    const created = new Date(Date.UTC(year, month, date));
    if (created.getUTCMonth() !== month || created.getUTCDate() !== date) {
      throw Error(`Invalid date ${date} for month ${month + 1} of ${year}.`);
    }
    return fromUtc(created);
  }

  today(): string {
    const now = new Date();
    return fromUtc(new Date(Date.UTC(now.getFullYear(), now.getMonth(), now.getDate())));
  }

  parse(value: unknown): string | null {
    if (typeof value !== 'string' || !value.trim()) {
      return null;
    }

    const typed = TYPED_DATE.exec(value.trim());
    if (typed) {
      return this.safeCreate(+typed[3], +typed[2] - 1, +typed[1]);
    }
    return this.isValid(value.trim()) ? value.trim() : INVALID;
  }

  format(date: string, displayFormat: unknown): string {
    if (!this.isValid(date)) {
      throw Error('IsoDateAdapter: cannot format an invalid date.');
    }

    const utc = toUtc(date);
    switch (displayFormat) {
      case 'month-year':
        return new Intl.DateTimeFormat(NAMES_LOCALE, { month: 'short', year: 'numeric', timeZone: 'UTC' }).format(utc);
      case 'month-year-long':
        return new Intl.DateTimeFormat(NAMES_LOCALE, { month: 'long', year: 'numeric', timeZone: 'UTC' }).format(utc);
      case 'long':
        return new Intl.DateTimeFormat(NAMES_LOCALE, { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' }).format(utc);
      default:
        return `${this.getDate(date)}. ${this.getMonth(date) + 1}. ${this.getYear(date)}.`;
    }
  }

  addCalendarYears(date: string, years: number): string {
    return this.addCalendarMonths(date, years * 12);
  }

  addCalendarMonths(date: string, months: number): string {
    const target = new Date(Date.UTC(this.getYear(date), this.getMonth(date) + months, 1));
    const lastDay = new Date(Date.UTC(target.getUTCFullYear(), target.getUTCMonth() + 1, 0)).getUTCDate();
    target.setUTCDate(Math.min(this.getDate(date), lastDay));
    return fromUtc(target);
  }

  addCalendarDays(date: string, days: number): string {
    const target = toUtc(date);
    target.setUTCDate(target.getUTCDate() + days);
    return fromUtc(target);
  }

  toIso8601(date: string): string {
    return date;
  }

  isDateInstance(value: unknown): boolean {
    return typeof value === 'string';
  }

  isValid(date: string): boolean {
    const parts = ISO_DATE.exec(date);
    if (!parts) {
      return false;
    }
    const utc = new Date(Date.UTC(+parts[1], +parts[2] - 1, +parts[3]));
    return utc.getUTCFullYear() === +parts[1] && utc.getUTCMonth() === +parts[2] - 1 && utc.getUTCDate() === +parts[3];
  }

  invalid(): string {
    return INVALID;
  }

  override deserialize(value: unknown): string | null {
    if (value === null || value === undefined || value === '') {
      return null;
    }
    return typeof value === 'string' && this.isValid(value) ? value : this.invalid();
  }

  private safeCreate(year: number, month: number, date: number): string {
    try {
      return this.createDate(year, month, date);
    } catch {
      return INVALID;
    }
  }
}

export function provideIsoDates(): Provider[] {
  return [
    { provide: DateAdapter, useClass: IsoDateAdapter },
    { provide: MAT_DATE_FORMATS, useValue: DATE_FORMATS }
  ];
}

export function isIsoDate(value: unknown): value is string {
  return typeof value === 'string' && ISO_DATE.test(value);
}

function toUtc(date: string): Date {
  return new Date(Date.UTC(+date.slice(0, 4), +date.slice(5, 7) - 1, +date.slice(8, 10)));
}

function fromUtc(date: Date): string {
  const month = String(date.getUTCMonth() + 1).padStart(2, '0');
  const day = String(date.getUTCDate()).padStart(2, '0');
  return `${String(date.getUTCFullYear()).padStart(4, '0')}-${month}-${day}`;
}
