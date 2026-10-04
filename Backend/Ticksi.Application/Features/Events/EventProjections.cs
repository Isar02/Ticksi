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
        Price = e.Price,
        TicketCount = e.TicketCount,
        Contact = e.Contact,
        EventCategoryName = e.EventCategory!.Name,
        EventCategoryPublicId = e.EventCategory.PublicId,
        LocationName = e.Location!.Name,
        EventTypeName = e.EventType!.Name,
        OrganizerCompanyName = e.OrganizerCompany!.Name
    };
}
