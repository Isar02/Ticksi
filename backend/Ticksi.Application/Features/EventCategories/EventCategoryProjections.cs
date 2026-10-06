using System.Linq.Expressions;
using Ticksi.Application.DTOs;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.EventCategories;

public static class EventCategoryProjections
{
    public static readonly Expression<Func<EventCategory, EventCategoryReadDto>> ToReadDto = c => new EventCategoryReadDto
    {
        PublicId = c.PublicId,
        Name = c.Name,
        Description = c.Description,
        PosterUrl = c.PosterUrl
    };
}
