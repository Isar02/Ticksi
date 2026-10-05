using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;
using Ticksi.Application.Options;

namespace Ticksi.Application.Features.Events.Commands.UploadEventPoster;

public class UploadEventPosterCommandHandler : IRequestHandler<UploadEventPosterCommand, EventPosterDto>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorageService _files;
    private readonly FileUploadOptions _uploadOptions;

    public UploadEventPosterCommandHandler(
        IAppDbContext context,
        ICurrentUser currentUser,
        IFileStorageService files,
        IOptions<FileUploadOptions> uploadOptions)
    {
        _context = context;
        _currentUser = currentUser;
        _files = files;
        _uploadOptions = uploadOptions.Value;
    }

    public async Task<EventPosterDto> Handle(UploadEventPosterCommand request, CancellationToken cancellationToken)
    {
        var editor = await EventEditor.ResolveAsync(_context, _currentUser, cancellationToken);

        var item = await _context.Events
            .FirstOrDefaultAsync(e => e.PublicId == request.PublicId, cancellationToken)
            ?? throw new NotFoundException("Event not found.");

        editor.EnsureCanManage(item.AppUserId);

        var previousUrl = item.PosterUrl;
        var posterUrl = await _files.SaveFileAsync(request.File!, _uploadOptions.EventPosterPath, cancellationToken);
        item.PosterUrl = posterUrl;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            await _files.DeleteFileAsync(posterUrl);

            if (e is DbUpdateConcurrencyException)
                throw new ConflictException("The event changed while the poster was uploading. Reload it and try again.");

            throw;
        }

        await EventPosterFiles.RemoveIfUnusedAsync(_context, _files, previousUrl);
        return new EventPosterDto { PosterUrl = posterUrl };
    }
}
