using AutoMapper;
using MediatR;
using Ticksi.Application.DTOs;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.EventCategories.Queries.GetEventCategories
{
    public class GetEventCategoriesQueryHandler : IRequestHandler<GetEventCategoriesQuery, PagedResult<EventCategoryReadDto>>
    {
        private readonly IEventCategoryRepository _repository;
        private readonly IMapper _mapper;

        public GetEventCategoriesQueryHandler(IEventCategoryRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PagedResult<EventCategoryReadDto>> Handle(GetEventCategoriesQuery request, CancellationToken cancellationToken)
        {
            var queryDto = new EventCategoryQueryDto
            {
                Search = request.Search,
                Filter = request.Filter,
                Page = request.Page,
                PageSize = request.PageSize
            };

            var pagedResult = await _repository.GetPagedCategoriesAsync(queryDto);

            var dtos = _mapper.Map<List<EventCategoryReadDto>>(pagedResult.Items);

            return new PagedResult<EventCategoryReadDto>
            {
                Items = dtos,
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize,
                TotalCount = pagedResult.TotalCount
            };
        }
    }
}

