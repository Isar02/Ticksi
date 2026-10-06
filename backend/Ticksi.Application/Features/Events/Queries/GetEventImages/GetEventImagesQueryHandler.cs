using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Events.Queries.GetEventImages
{
    public class GetEventImagesQueryHandler
        : IRequestHandler<GetEventImagesQuery, List<string>>
    {
        private readonly IAppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public GetEventImagesQueryHandler(
            IAppDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<List<string>> Handle(
            GetEventImagesQuery request,
            CancellationToken cancellationToken)
        {
            // 1️⃣ Provjera da li event postoji
            var eventExists = await _context.Events
                .AnyAsync(e => e.PublicId == request.EventId, cancellationToken);

            if (!eventExists)
                throw new NotFoundException("Event not found.");

            // 2️⃣ Folder gdje se već snimaju slike
            // wwwroot/images/events
            var imagesPath = Path.Combine(
                _environment.WebRootPath,
                "images",
                "events",
                request.EventId.ToString()
            );

            if (!Directory.Exists(imagesPath))
                return new List<string>();

            // 3️⃣ Učitaj sve dozvoljene slike
            var files = Directory.GetFiles(imagesPath)
                .Where(f => AllowedExtensions.Contains(Path.GetExtension(f)))
                .Select(f => $"/images/events/{request.EventId}/{Path.GetFileName(f)}")
                .ToList();

            return files;
        }
    }
}
