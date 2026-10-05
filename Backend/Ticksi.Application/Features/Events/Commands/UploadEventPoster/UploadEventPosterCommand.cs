using MediatR;
using Microsoft.AspNetCore.Http;

namespace Ticksi.Application.Features.Events.Commands.UploadEventPoster;

public class UploadEventPosterCommand : IRequest<EventPosterDto>
{
    public Guid PublicId { get; set; }
    public IFormFile? File { get; set; }
}
