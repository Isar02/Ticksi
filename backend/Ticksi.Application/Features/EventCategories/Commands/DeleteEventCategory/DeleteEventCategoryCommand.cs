using MediatR;

namespace Ticksi.Application.Features.EventCategories.Commands.DeleteEventCategory
{
    public class DeleteEventCategoryCommand : IRequest
    {
        public Guid PublicId { get; set; }
    }
}
