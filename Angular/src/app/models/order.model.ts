export const ORDER_MAX_QUANTITY = 10;

export type OrderStatus = 'Pending' | 'Paid' | 'Cancelled';

export interface TicketTypeOffer {
  publicId: string;
  name: string;
  price: number;
  available: number;
}

export interface OrderLineInput {
  ticketTypeId: string;
  quantity: number;
}

export interface OrderItem {
  eventId: string;
  eventName: string;
  eventDate: string;
  ticketTypeName: string;
  quantity: number;
  unitPrice: number;
}

export interface Order {
  publicId: string;
  status: OrderStatus;
  totalAmount: number;
  createdAtUtc: string;
  items: OrderItem[];
}
