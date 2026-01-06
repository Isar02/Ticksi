using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Ticksi.Application.DTOs;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Events.Queries.GetEventById
{
    public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventReadDto?>
    {
        private readonly IEventRepository _repository;
        private readonly IMapper _mapper;

        public GetEventByIdQueryHandler(IEventRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<EventReadDto?> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
        {
            // Ovdje treba repo metoda koja vraća Event + related entitete (Category, Location, Type, Organizer)
            var ev = await _repository.GetByPublicIdAsync(request.EventId, cancellationToken);

            if (ev == null) return null;

            return _mapper.Map<EventReadDto>(ev);
        }
    }
}


