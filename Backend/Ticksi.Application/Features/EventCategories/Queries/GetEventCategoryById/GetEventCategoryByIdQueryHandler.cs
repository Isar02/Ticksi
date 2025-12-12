using AutoMapper;
using MediatR;
using Ticksi.Application.DTOs;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.EventCategories.Queries.GetEventCategoryById
{
    public class GetEventCategoryByIdQueryHandler : IRequestHandler<GetEventCategoryByIdQuery, EventCategoryReadDto?>
    {
        private readonly IEventCategoryRepository _repository;
        private readonly IMapper _mapper;

        public GetEventCategoryByIdQueryHandler(IEventCategoryRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<EventCategoryReadDto?> Handle(GetEventCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            var category = await _repository.GetByPublicIDAsync(request.PublicId);
            if (category == null)
                return null;

            return _mapper.Map<EventCategoryReadDto>(category);
        }
    }
}

