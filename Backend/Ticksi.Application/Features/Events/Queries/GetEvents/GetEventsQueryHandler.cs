using AutoMapper;
using MediatR;
using Ticksi.Application.DTOs;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Events.Queries.GetEvents
{
    public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, PagedResult<EventReadDto>>
    {
        private readonly IEventRepository _repository;
        private readonly IMapper _mapper;

        public GetEventsQueryHandler(IEventRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PagedResult<EventReadDto>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
        {
            var queryDto = new EventQueryDto
            {
                Search = request.Search,
                CategoryId = request.CategoryId,
                DateFrom = request.DateFrom,
                DateTo = request.DateTo,
                MinPrice = request.MinPrice,
                MaxPrice = request.MaxPrice,
                SortBy = request.SortBy,
                SortDescending = request.SortDescending,
                Page = request.Page,
                PageSize = request.PageSize
            };

            var pagedResult = await _repository.GetPagedEventsAsync(queryDto);

            var dtos = _mapper.Map<List<EventReadDto>>(pagedResult.Items);

            return new PagedResult<EventReadDto>
            {
                Items = dtos,
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize,
                TotalCount = pagedResult.TotalCount
            };
        }
    }
}

