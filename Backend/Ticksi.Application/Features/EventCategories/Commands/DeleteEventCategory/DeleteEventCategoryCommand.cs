using MediatR;

namespace Ticksi.Application.Features.EventCategories.Commands.DeleteEventCategory
{
    public class DeleteEventCategoryCommand : IRequest<bool>
    {
        public Guid PublicId { get; set; }
    }
}

