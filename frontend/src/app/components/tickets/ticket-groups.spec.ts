import { Ticket } from '../../models/ticket.model';
import { groupByEvent } from './ticket-groups';

describe('groupByEvent', () => {
  const now = new Date('2026-10-06T12:00:00+02:00');

  function ticket(code: string, eventId: string, eventDate: string): Ticket {
    return {
      publicId: code,
      code,
      status: 'Valid',
      ticketTypeName: 'Standard',
      eventId,
      eventName: `Event ${eventId}`,
      eventDate,
      venueName: 'Zetra',
      venueCity: 'Sarajevo',
      orderId: 'order'
    };
  }

  it('groups the tickets of one event and keeps upcoming events soonest first', () => {
    const wallet = groupByEvent(
      [
        ticket('A1', 'a', '2026-10-08T20:00:00'),
        ticket('A2', 'a', '2026-10-08T20:00:00'),
        ticket('B1', 'b', '2026-12-01T19:00:00')
      ],
      now
    );

    expect(wallet.upcoming.map(group => [group.eventId, group.tickets.map(t => t.code)])).toEqual([
      ['a', ['A1', 'A2']],
      ['b', ['B1']]
    ]);
    expect(wallet.upcoming[0]).toEqual(jasmine.objectContaining({ eventName: 'Event a', venueName: 'Zetra', venueCity: 'Sarajevo' }));
    expect(wallet.past).toEqual([]);
  });

  it('sets past events apart, the most recent first', () => {
    const wallet = groupByEvent(
      [
        ticket('O1', 'old', '2026-05-01T20:00:00'),
        ticket('R1', 'recent', '2026-10-06T11:00:00'),
        ticket('S1', 'soon', '2026-10-06T13:00:00')
      ],
      now
    );

    expect(wallet.past.map(group => group.eventId)).toEqual(['recent', 'old']);
    expect(wallet.upcoming.map(group => group.eventId)).toEqual(['soon']);
  });
});
