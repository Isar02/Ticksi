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
  venues: VenueOption[];
  eventTypes: NamedOption[];
  organizerCompanies: NamedOption[];
}

