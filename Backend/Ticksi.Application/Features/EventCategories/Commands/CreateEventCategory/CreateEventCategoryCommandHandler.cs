using AutoMapper;
using MediatR;
using Ticksi.Application.DTOs;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.EventCategories.Commands.CreateEventCategory
{
    public class CreateEventCategoryCommandHandler : IRequestHandler<CreateEventCategoryCommand, EventCategoryReadDto>
    {
        private readonly IEventCategoryRepository _repository;
        private readonly IMapper _mapper;

        public CreateEventCategoryCommandHandler(IEventCategoryRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<EventCategoryReadDto> Handle(CreateEventCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = new EventCategory
            {
                Name = request.Name,
                Description = request.Description
            };

            await _repository.AddAsync(category);

            return _mapper.Map<EventCategoryReadDto>(category);
        }
    }
}

