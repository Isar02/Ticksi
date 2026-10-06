import { Event } from '../../models/event.model';

export function isOnSale(event: Event, now = Date.now()): boolean {
  return event.lowestPrice !== null && event.availableTickets > 0 && Date.parse(event.date) > now;
}
