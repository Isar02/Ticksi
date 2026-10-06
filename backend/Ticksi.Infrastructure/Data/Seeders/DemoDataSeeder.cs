using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ticksi.Application.Interfaces;
using Ticksi.Application.Options;
using Ticksi.Domain.Entities;
using Ticksi.Infrastructure.Options;

namespace Ticksi.Infrastructure.Data.Seeders;

public sealed class DemoDataSeeder(
    AppDbContext context,
    IPasswordHasher passwordHasher,
    IWebHostEnvironment environment,
    IOptions<FileUploadOptions> fileUploadOptions,
    IOptions<SeedingOptions> seedingOptions,
    TimeProvider timeProvider)
{
    private const string OrganizerEmail = "organizer@ticksi.com";

    private static readonly string ImagesRoot = Path.Combine(AppContext.BaseDirectory, "Data", "Seeders", "Images");

    private readonly FileUploadOptions _uploads = fileUploadOptions.Value;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var users = await AddMissingUsersAsync(cancellationToken);
        var company = await AddMissingCompanyAsync(cancellationToken);
        var venues = await AddMissingVenuesAsync(cancellationToken);
        var categories = await AddMissingCategoriesAsync(cancellationToken);
        await AddEventsAsync(users[OrganizerEmail], company, venues, categories, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<string, AppUser>> AddMissingUsersAsync(CancellationToken cancellationToken)
    {
        var roles = await context.Roles.ToDictionaryAsync(r => r.Name, cancellationToken);

        AppUser User(string firstName, string lastName, string email, string phone, string role)
        {
            var user = new AppUser
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = phone,
                RegistrationDate = timeProvider.GetUtcNow().UtcDateTime,
                Role = roles[role]
            };
            user.PasswordHash = passwordHasher.Hash(user, seedingOptions.Value.DemoPassword);
            return user;
        }

        AppUser[] demoUsers =
        [
            User("Amar", "Hodžić", "admin@ticksi.com", "+38761100001", Role.Names.Admin),
            User("Lejla", "Begić", OrganizerEmail, "+38761100002", Role.Names.Organizer),
            User("Emir", "Kovačević", "user@ticksi.com", "+38761100003", Role.Names.User)
        ];

        var emails = demoUsers.Select(u => u.Email).ToList();
        var existing = await context.AppUsers.Where(u => emails.Contains(u.Email)).ToListAsync(cancellationToken);
        return AddMissing(context.AppUsers, existing, demoUsers, u => u.Email);
    }

    private async Task<OrganizerCompany> AddMissingCompanyAsync(CancellationToken cancellationToken)
    {
        var company = new OrganizerCompany
        {
            Name = "Ticksi Live d.o.o.",
            Email = "live@ticksi.com",
            Phone = "+38733100200",
            Address = "Zmaja od Bosne 7, Sarajevo",
            TaxId = "4200000000001"
        };

        var existing = await context.OrganizerCompanies.Where(c => c.Name == company.Name).ToListAsync(cancellationToken);
        return AddMissing(context.OrganizerCompanies, existing, [company], c => c.Name)[company.Name];
    }

    private async Task<Dictionary<string, Location>> AddMissingVenuesAsync(CancellationToken cancellationToken)
    {
        Location[] venues =
        [
            new() { Name = "Sarajevo Arena", City = "Sarajevo", Address = "Alipašina 5", Capacity = 12000 },
            new() { Name = "City Theatre Mostar", City = "Mostar", Address = "Kralja Tomislava 12", Capacity = 650 },
            new() { Name = "Tuzla City Stadium", City = "Tuzla", Address = "Stadionska 1", Capacity = 15000 },
            new() { Name = "Banja Luka Congress Centre", City = "Banja Luka", Address = "Bulevar vojvode Stepe 30", Capacity = 1200 },
            new() { Name = "Gallery Nova", City = "Sarajevo", Address = "Ferhadija 18", Capacity = 300 },
            new() { Name = "Riverside Summer Stage", City = "Zenica", Address = "Kej 10", Capacity = 8000 }
        ];

        var names = venues.Select(v => v.Name).ToList();
        var existing = await context.Locations.Where(l => names.Contains(l.Name)).ToListAsync(cancellationToken);
        return AddMissing(context.Locations, existing, venues, l => l.Name);
    }

    private async Task<Dictionary<string, EventCategory>> AddMissingCategoriesAsync(CancellationToken cancellationToken)
    {
        EventCategory Category(string name, string description, string poster) => new()
        {
            Name = name,
            Description = description,
            PosterUrl = CopyImage(Path.Combine("categories", poster), _uploads.CategoryPosterPath)
        };

        EventCategory[] categories =
        [
            Category("Music", "Concerts and live performances of every genre.", "music.jpg"),
            Category("Theatre", "Plays, opera, ballet and stand-up comedy.", "theatre.jpg"),
            Category("Sports", "Matches, races and tournaments.", "sports.jpg"),
            Category("Conference", "Talks, panels and professional meetups.", "conference.jpg"),
            Category("Festival", "Multi-day programmes with many performers.", "festival.jpg"),
            Category("Art", "Exhibitions, installations and gallery nights.", "art.jpg")
        ];

        var names = categories.Select(c => c.Name).ToList();
        var existing = await context.EventCategories.Where(c => names.Contains(c.Name)).ToListAsync(cancellationToken);
        return AddMissing(context.EventCategories, existing, categories, c => c.Name);
    }

    private async Task AddEventsAsync(
        AppUser organizer,
        OrganizerCompany company,
        Dictionary<string, Location> venues,
        Dictionary<string, EventCategory> categories,
        CancellationToken cancellationToken)
    {
        var demoByName = DemoEvents.ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase);
        var names = demoByName.Keys.ToList();
        var seeded = await context.Events
            .Where(e => names.Contains(e.Name) && e.AppUser!.Email == OrganizerEmail)
            .Select(e => new { e.Name, e.PublicId })
            .ToListAsync(cancellationToken);

        if (seeded.Count > 0)
        {
            foreach (var item in seeded)
            {
                if (demoByName.TryGetValue(item.Name, out var demo))
                    CopyEventImages(demo, item.PublicId);
            }

            return;
        }

        var types = await context.EventTypes.ToDictionaryAsync(t => t.Name, cancellationToken);
        var today = timeProvider.GetUtcNow().UtcDateTime.Date;

        foreach (var demo in DemoEvents)
        {
            var venue = venues[demo.Venue];
            var item = new Event
            {
                Name = demo.Name,
                Description = demo.Description,
                Date = today.AddDays(demo.DaysAhead).AddHours(demo.Hour),
                Contact = company.Email,
                AppUser = organizer,
                OrganizerCompany = company,
                EventType = types[demo.Type],
                EventCategory = categories[demo.Category],
                Location = venue,
                TicketTypes =
                [
                    new() { Name = "Standard", Price = demo.Price, Quantity = venue.Capacity * 9 / 10 },
                    new() { Name = "VIP", Price = demo.Price * 2.5m, Quantity = venue.Capacity / 10 }
                ]
            };

            item.PosterUrl = CopyEventImages(demo, item.PublicId);
            context.Events.Add(item);
        }
    }

    private static Dictionary<string, T> AddMissing<T>(DbSet<T> set, IEnumerable<T> existing, IEnumerable<T> wanted, Func<T, string> key)
        where T : class
    {
        var byKey = existing
            .GroupBy(key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var item in wanted.Where(w => !byKey.ContainsKey(key(w))))
        {
            set.Add(item);
            byKey[key(item)] = item;
        }

        return byKey;
    }

    private string CopyImage(string source, string targetFolder)
    {
        var fileName = Path.GetFileName(source);
        var target = Path.Combine(WebRoot, targetFolder, fileName);

        if (!File.Exists(target))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(Path.Combine(ImagesRoot, source), target);
        }

        return $"/{targetFolder}/{fileName}";
    }

    private string CopyEventImages(DemoEvent demo, Guid eventPublicId)
    {
        if (demo.Gallery is not null)
        {
            var folder = $"{_uploads.EventPosterPath}/{eventPublicId}";
            foreach (var file in Directory.GetFiles(Path.Combine(ImagesRoot, "gallery"), $"{demo.Gallery}-*.jpg"))
                CopyImage(Path.Combine("gallery", Path.GetFileName(file)), folder);
        }

        return CopyImage(Path.Combine("events", demo.Poster), _uploads.EventPosterPath);
    }

    private string WebRoot => string.IsNullOrEmpty(environment.WebRootPath)
        ? Path.Combine(environment.ContentRootPath, "wwwroot")
        : environment.WebRootPath;

    private sealed record DemoEvent(
        string Name, string Category, string Type, string Venue, int DaysAhead, int Hour,
        decimal Price, string Poster, string Description, string? Gallery = null);

    private static readonly DemoEvent[] DemoEvents =
    [
        new("Sarajevo Rock Night", "Music", "Concert", "Sarajevo Arena", 5, 20, 35m, "music-1.jpg",
            "Five regional rock bands on one stage, from early evening until late.", Gallery: "concert"),
        new("Jazz by the River", "Music", "Concert", "Riverside Summer Stage", 12, 21, 25m, "music-2.jpg",
            "An open-air evening of modern jazz with a quartet and guest soloists."),
        new("Symphony Under the Stars", "Music", "Concert", "Sarajevo Arena", 26, 19, 40m, "music-1.jpg",
            "The philharmonic orchestra plays film scores and classical favourites."),
        new("Indie Weekend Live", "Music", "Concert", "Riverside Summer Stage", 41, 20, 20m, "music-2.jpg",
            "Young indie and alternative acts presenting their new albums."),
        new("Sevdah Evening", "Music", "Concert", "City Theatre Mostar", 58, 20, 30m, "music-1.jpg",
            "Traditional sevdalinka songs performed with a full acoustic ensemble."),
        new("Electronic Arena", "Music", "Concert", "Sarajevo Arena", 77, 22, 45m, "music-2.jpg",
            "Headlining DJs and a light show that runs past midnight."),

        new("Hamlet", "Theatre", "Theatre", "City Theatre Mostar", 7, 19, 15m, "theatre-1.jpg",
            "A new staging of Shakespeare's tragedy in a modern setting."),
        new("The Marriage of Figaro", "Theatre", "Theatre", "City Theatre Mostar", 19, 19, 30m, "theatre-2.jpg",
            "Mozart's comic opera with the national opera ensemble."),
        new("Stand-up Comedy Night", "Theatre", "Theatre", "Banja Luka Congress Centre", 33, 21, 12m, "theatre-1.jpg",
            "Four comedians, one microphone and two hours of new material."),
        new("Swan Lake", "Theatre", "Theatre", "Sarajevo Arena", 52, 19, 35m, "theatre-2.jpg",
            "The classic ballet performed by a visiting company with live orchestra."),
        new("Improv Theatre Jam", "Theatre", "Theatre", "Gallery Nova", 69, 20, 10m, "theatre-1.jpg",
            "An improvised show built entirely from the audience's suggestions."),

        new("Derby Day Football", "Sports", "Sport", "Tuzla City Stadium", 4, 18, 10m, "sports-1.jpg",
            "The season's biggest league match between two local rivals."),
        new("Basketball Cup Final", "Sports", "Sport", "Sarajevo Arena", 16, 20, 15m, "sports-2.jpg",
            "The two best teams of the season meet in the national cup final."),
        new("City Half Marathon", "Sports", "Sport", "Tuzla City Stadium", 29, 9, 8m, "sports-1.jpg",
            "A 21 km route through the city centre, finishing inside the stadium."),
        new("Handball Championship", "Sports", "Sport", "Sarajevo Arena", 44, 19, 10m, "sports-2.jpg",
            "Group stage of the regional handball championship."),
        new("Youth Football Tournament", "Sports", "Sport", "Tuzla City Stadium", 63, 10, 5m, "sports-1.jpg",
            "Under-17 teams from twelve clubs compete over one weekend."),
        new("Volleyball All-Star Game", "Sports", "Sport", "Sarajevo Arena", 85, 18, 12m, "sports-2.jpg",
            "League stars split into two teams for an exhibition match."),

        new("Tech Summit Banja Luka", "Conference", "Conference", "Banja Luka Congress Centre", 10, 9, 60m, "conference-1.jpg",
            "Talks on cloud, AI and product engineering from regional companies."),
        new("Startup Pitch Day", "Conference", "Conference", "Banja Luka Congress Centre", 23, 10, 20m, "conference-2.jpg",
            "Twenty early-stage startups pitch to investors and mentors."),
        new("Design Systems Workshop", "Conference", "Workshop", "Gallery Nova", 37, 10, 45m, "conference-1.jpg",
            "A full-day hands-on workshop on building and maintaining design systems."),
        new("Digital Marketing Forum", "Conference", "Conference", "Banja Luka Congress Centre", 55, 9, 50m, "conference-2.jpg",
            "Case studies and panels on content, search and paid campaigns."),
        new("Data Science Meetup", "Conference", "Workshop", "Gallery Nova", 72, 17, 15m, "conference-1.jpg",
            "An evening of short talks and live coding with real datasets."),

        new("Summer Sound Festival", "Festival", "Festival", "Riverside Summer Stage", 9, 16, 55m, "festival-1.jpg",
            "Three days, two stages and more than thirty performers by the river.", Gallery: "festival"),
        new("Street Food & Music Fest", "Festival", "Festival", "Riverside Summer Stage", 30, 12, 15m, "festival-2.jpg",
            "Food trucks from the whole region with live bands all afternoon."),
        new("Film Under the Open Sky", "Festival", "Festival", "Sarajevo Arena", 47, 20, 18m, "festival-1.jpg",
            "A week of open-air screenings of new European films."),
        new("Folk Heritage Days", "Festival", "Festival", "City Theatre Mostar", 66, 17, 10m, "festival-2.jpg",
            "Folk dance ensembles, crafts and traditional music."),
        new("Autumn Lights Festival", "Festival", "Festival", "Riverside Summer Stage", 94, 18, 22m, "festival-1.jpg",
            "Light installations, music and night markets along the riverside."),

        new("Contemporary Art Biennial", "Art", "Exhibition", "Gallery Nova", 6, 11, 8m, "art-1.jpg",
            "Works by forty contemporary artists from the region."),
        new("Photography Night", "Art", "Exhibition", "Gallery Nova", 21, 19, 6m, "art-2.jpg",
            "Documentary and street photography with talks by the authors."),
        new("Sculpture in the Park", "Art", "Exhibition", "Riverside Summer Stage", 39, 10, 5m, "art-1.jpg",
            "Large outdoor sculptures shown along the summer stage promenade."),
        new("Painting Workshop for Beginners", "Art", "Workshop", "Gallery Nova", 60, 17, 25m, "art-2.jpg",
            "Learn the basics of acrylic painting; all materials are provided."),
        new("Digital Art Showcase", "Art", "Exhibition", "Banja Luka Congress Centre", 81, 12, 7m, "art-1.jpg",
            "Interactive and generative digital art on large screens.")
    ];
}
