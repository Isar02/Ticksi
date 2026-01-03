using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Ticksi.Application.Features.Events.Queries.GetEventImages
{
    public record GetEventImagesQuery(Guid EventId) : IRequest<List<string>>
    {
    }
}
