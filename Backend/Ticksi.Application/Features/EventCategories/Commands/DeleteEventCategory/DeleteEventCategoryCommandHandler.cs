using MediatR;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.EventCategories.Commands.DeleteEventCategory
{
    public class DeleteEventCategoryCommandHandler : IRequestHandler<DeleteEventCategoryCommand, bool>
    {
        private readonly IEventCategoryRepository _repository;

        public DeleteEventCategoryCommandHandler(IEventCategoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(DeleteEventCategoryCommand request, CancellationToken cancellationToken)
        {
            var existingCategory = await _repository.GetByPublicIDAsync(request.PublicId);
            if (existingCategory == null)
                return false;

            await _repository.DeleteAsync(existingCategory);
            return true;
        }
    }
}

