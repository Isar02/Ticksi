using MediatR;
using Ticksi.Application.DTOs;

namespace Ticksi.Application.Features.Favorites.Queries.GetFavoriteEvents;

public record GetFavoriteEventsQuery : IRequest<List<EventReadDto>>;
