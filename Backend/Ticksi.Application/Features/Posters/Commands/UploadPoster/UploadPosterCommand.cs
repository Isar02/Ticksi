using MediatR;
using Microsoft.AspNetCore.Http;

namespace Ticksi.Application.Features.Posters.Commands.UploadPoster;

public class UploadPosterCommand : IRequest<UploadPosterResponse>
{
    public Guid EventPublicId { get; set; }
    public IFormFile File { get; set; } = null!;
}
