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

