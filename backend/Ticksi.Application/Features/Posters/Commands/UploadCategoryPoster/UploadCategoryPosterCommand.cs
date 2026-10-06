using MediatR;
using Microsoft.AspNetCore.Http;

namespace Ticksi.Application.Features.Categories.Commands.UploadCategoryPoster
{
    public class UploadCategoryPosterCommand : IRequest<UploadCategoryPosterResponse>
    {
        public IFormFile File { get; set; } = default!;
    }
}
