using System.Linq.Expressions;
using Ticksi.Application.DTOs;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Events;

public static class EventProjections
{
    public static readonly Expression<Func<Event, EventReadDto>> ToReadDto = e => new EventReadDto
    {
        PublicId = e.PublicId,
        Name = e.Name,
        Description = e.Description,
        Date = e.Date,
        Contact = e.Contact,
        PosterUrl = e.PosterUrl,
        LowestPrice = e.TicketTypes.Min(t => (decimal?)t.Price),
        AvailableTickets = e.TicketTypes.Sum(t => t.Quantity - t.QuantityReserved),
        EventCategoryName = e.EventCategory!.Name,
        EventCategoryPublicId = e.EventCategory.PublicId,
        LocationName = e.Location!.Name,
        EventTypeName = e.EventType!.Name,
        OrganizerCompanyName = e.OrganizerCompany!.Name
    };
}
