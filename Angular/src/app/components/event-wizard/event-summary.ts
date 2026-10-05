import { EventFormOptions, VenueOption } from '../../models/event.model';
import { EventWizardForm } from './event-wizard-form';

export interface EventSummary {
  name: string;
  description: string;
  contact: string;
  categoryName: string | null;
  eventTypeName: string | null;
  companyName: string | null;
  venue: VenueOption | null;
  startsAt: Date | null;
  tickets: { name: string; price: number | null; quantity: number | null }[];
  ticketTotal: number;
  lowestPrice: number | null;
}

export function summarize(form: EventWizardForm, options: EventFormOptions): EventSummary {
  const { details, schedule, tickets } = form.getRawValue();
  const nameOf = (list: { publicId: string; name: string }[], id: string) => list.find(item => item.publicId === id)?.name ?? null;
  const prices = tickets.map(ticket => ticket.price).filter((price): price is number => price !== null);

  return {
    name: details.name.trim(),
    description: details.description.trim(),
    contact: details.contact.trim(),
    categoryName: nameOf(options.categories, details.categoryId),
    eventTypeName: nameOf(options.eventTypes, details.eventTypeId),
    companyName: nameOf(options.organizerCompanies, details.organizerCompanyId),
    venue: options.venues.find(venue => venue.publicId === schedule.locationId) ?? null,
    startsAt: schedule.date && schedule.time ? new Date(`${schedule.date}T${schedule.time}`) : null,
    tickets: tickets.map(({ name, price, quantity }) => ({ name: name.trim(), price, quantity })),
    ticketTotal: tickets.reduce((total, ticket) => total + (ticket.quantity ?? 0), 0),
    lowestPrice: prices.length > 0 ? Math.min(...prices) : null
  };
}
