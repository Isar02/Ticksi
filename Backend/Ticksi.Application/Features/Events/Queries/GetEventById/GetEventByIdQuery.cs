using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Ticksi.Application.DTOs;

namespace Ticksi.Application.Features.Events.Queries.GetEventById
{
    public record GetEventByIdQuery(Guid EventId) : IRequest<EventReadDto?>
    {
    }
}
