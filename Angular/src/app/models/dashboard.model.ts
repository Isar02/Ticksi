import { OrderStatus } from './order.model';

export interface Dashboard {
  upcomingTickets: number;
  nextEvent: DashboardEvent | null;
  favorites: number;
  recentOrders: DashboardOrder[];
  sales: DashboardSales | null;
}

export interface DashboardEvent {
  eventId: string;
  name: string;
  date: string;
  venueName: string;
  venueCity: string;
  tickets: number;
}

export interface DashboardOrder {
  orderId: string;
  eventNames: string[];
  tickets: number;
  totalAmount: number;
  status: OrderStatus;
  createdAtUtc: string;
}

export interface DashboardSales {
  allEvents: boolean;
  upcomingEvents: number;
  ticketsSold: number;
  revenue: number;
  activeUsers: number | null;
  topEvents: DashboardTopEvent[];
}

export interface DashboardTopEvent {
  eventId: string;
  name: string;
  date: string;
  ticketsSold: number;
  revenue: number;
}

export function orderEventsLabel(eventNames: readonly string[]): string {
  if (eventNames.length === 0) return 'Order';
  return eventNames.length === 1 ? eventNames[0] : `${eventNames[0]} + ${eventNames.length - 1} more`;
}
