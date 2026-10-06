using MediatR;
using Ticksi.Application.Common;

namespace Ticksi.Application.Features.Events.Commands.UploadEventPoster;

public class UploadEventPosterCommand : IRequest<EventPosterDto>
{
    public Guid PublicId { get; set; }
    public FileUpload? File { get; set; }
}
