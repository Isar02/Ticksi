export type TicketStatus = 'Valid' | 'Used' | 'Cancelled';

export interface Ticket {
  publicId: string;
  code: string;
  status: TicketStatus;
  ticketTypeName: string;
  eventId: string;
  eventName: string;
  eventDate: string;
  venueName: string;
  venueCity: string;
  orderId: string;
}

export interface EventTickets {
  eventId: string;
  eventName: string;
  eventDate: string;
  venueName: string;
  venueCity: string;
  tickets: Ticket[];
}
