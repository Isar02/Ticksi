export interface Event {
  publicId: string;
  name: string;
  description: string;
  date: string;
  contact: string;
  posterUrl: string | null;
  lowestPrice: number | null;
  availableTickets: number;
  eventCategoryName: string;
  eventCategoryPublicId: string;
  locationName: string;
  eventTypeName: string;
  organizerCompanyName: string;
}

export interface ManagedEvent {
  publicId: string;
  name: string;
  date: string;
  categoryName: string;
  venueName: string;
  ticketsSold: number;
  ticketsTotal: number;
}

export const EVENT_PERIODS = ['upcoming', 'past'] as const;
export type EventPeriod = (typeof EVENT_PERIODS)[number];

export const MANAGED_EVENT_SORTS = ['name', 'date', 'venue', 'sold'] as const;
export type ManagedEventSort = (typeof MANAGED_EVENT_SORTS)[number];

export interface ManagedEventsQuery {
  name?: string;
  categoryId?: string;
  locationId?: string;
  dateFrom?: string;
  dateTo?: string;
  period?: EventPeriod;
  sortBy?: ManagedEventSort;
  sortDescending?: boolean;
  page: number;
  pageSize: number;
}

export interface NamedOption {
  publicId: string;
  name: string;
}

export interface VenueOption extends NamedOption {
  city: string;
  capacity: number;
}

export interface EventFormOptions {
  categories: NamedOption[];
  venues: VenueOption[];
  eventTypes: NamedOption[];
  organizerCompanies: NamedOption[];
}


export interface TicketTypeInput {
  publicId: string | null;
  name: string;
  price: number;
  quantity: number;
}

export interface EventInput {
  name: string;
  description: string;
  date: string;
  contact: string;
  categoryId: string;
  eventTypeId: string;
  locationId: string;
  organizerCompanyId: string;
  ticketTypes: TicketTypeInput[];
}

export interface TicketTypeForEdit {
  publicId: string;
  name: string;
  price: number;
  quantity: number;
  quantityReserved: number;
}

export interface EventForEdit extends Omit<EventInput, 'ticketTypes'> {
  publicId: string;
  categoryName: string;
  posterUrl: string | null;
  ticketTypes: TicketTypeForEdit[];
}

export interface EventPoster {
  posterUrl: string;
}
