using MediatR;

namespace Ticksi.Application.Features.EventCategories.Commands.UpdateEventCategory
{
    public class UpdateEventCategoryCommand : IRequest<bool>
    {
        public Guid PublicId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}

