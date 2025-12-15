using MediatR;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.EventCategories.Commands.UpdateEventCategory
{
    public class UpdateEventCategoryCommandHandler : IRequestHandler<UpdateEventCategoryCommand, bool>
    {
        private readonly IEventCategoryRepository _repository;

        public UpdateEventCategoryCommandHandler(IEventCategoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(UpdateEventCategoryCommand request, CancellationToken cancellationToken)
        {
            var existingCategory = await _repository.GetByPublicIDAsync(request.PublicId);
            if (existingCategory == null)
                return false;

            existingCategory.Name = request.Name;
            existingCategory.Description = request.Description;
            existingCategory.PosterUrl = request.PosterUrl;

            await _repository.UpdateAsync(existingCategory);
            return true;
        }
    }
}

