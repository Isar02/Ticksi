import { EventTickets, Ticket } from '../../models/ticket.model';

export interface TicketWallet {
  upcoming: EventTickets[];
  past: EventTickets[];
}

// Tickets arrive sorted by event date; upcoming events stay soonest first, past ones turn most recent first.
export function groupByEvent(tickets: Ticket[], now: Date): TicketWallet {
  const groups = new Map<string, EventTickets>();
  for (const ticket of tickets) {
    const group = groups.get(ticket.eventId);
    if (group) {
      group.tickets.push(ticket);
    } else {
      const { eventId, eventName, eventDate, venueName, venueCity } = ticket;
      groups.set(eventId, { eventId, eventName, eventDate, venueName, venueCity, tickets: [ticket] });
    }
  }

  const all = [...groups.values()];
  const isPast = (group: EventTickets) => new Date(group.eventDate).getTime() < now.getTime();
  return { upcoming: all.filter(group => !isPast(group)), past: all.filter(isPast).reverse() };
}
