using Microsoft.Extensions.Options;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Events.Commands.UploadEventPoster;
using Ticksi.Application.Options;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.EventTests.UnitTests;

public class UploadEventPosterCommandHandlerTests : EventHandlerTestBase
{
    private static readonly IOptions<FileUploadOptions> UploadOptions = Options.Create(new FileUploadOptions
    {
        AllowedImageTypes = [".png"],
        EventPosterPath = "images/events",
        CategoryPosterPath = "images/categories"
    });

    [Fact]
    public async Task Handle_OwnEvent_StoresThePosterAndSavesItsAddress()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var item = await AddEventAsync(organizer, await AddReferencesAsync());

        var result = await UploadAsync(organizer, item.PublicId);

        Assert.StartsWith("/images/events/", result.PosterUrl);
        Assert.EndsWith(".png", result.PosterUrl);
        Assert.Equal([result.PosterUrl], Files.Files);
        Assert.Equal(result.PosterUrl, (await FindEventAsync(item.PublicId))!.PosterUrl);
    }

    [Fact]
    public async Task Handle_ReplacingAPoster_RemovesTheOldFile()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var item = await AddEventAsync(organizer, await AddReferencesAsync(), posterUrl: "/images/events/old.png");
        Files.Add("/images/events/old.png");

        var result = await UploadAsync(organizer, item.PublicId);

        Assert.Equal([result.PosterUrl], Files.Files);
    }

    [Fact]
    public async Task Handle_OldPosterSharedWithAnotherEvent_KeepsTheOldFile()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        var item = await AddEventAsync(organizer, references, posterUrl: "/images/events/shared.png");
        await AddEventAsync(organizer, references, name: "Encore", posterUrl: "/images/events/shared.png");
        Files.Add("/images/events/shared.png");

        await UploadAsync(organizer, item.PublicId);

        Assert.Contains("/images/events/shared.png", Files.Files);
    }

    [Fact]
    public async Task Handle_SaveFails_RemovesTheNewFileAndKeepsTheOldPoster()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var item = await AddEventAsync(organizer, await AddReferencesAsync(), posterUrl: "/images/events/old.png");
        Files.Add("/images/events/old.png");
        var failingSave = new BeforeSaveInterceptor(1, _ => throw new DbUpdateException("The database is unavailable."));

        await Assert.ThrowsAsync<DbUpdateException>(() => UploadAsync(organizer, item.PublicId, failingSave));

        Assert.Equal(["/images/events/old.png"], Files.Files);
        Assert.Equal("/images/events/old.png", (await FindEventAsync(item.PublicId))!.PosterUrl);
    }

    [Fact]
    public async Task Handle_PosterReplacedAtTheSameTime_ThrowsConflictAndRemovesOnlyItsOwnFile()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var item = await AddEventAsync(organizer, await AddReferencesAsync(), posterUrl: "/images/events/old.png");
        Files.Add("/images/events/old.png");
        var parallelReplace = new BeforeSaveInterceptor(1, async cancellationToken =>
        {
            await using var other = Database.CreateContext();
            (await other.Events.SingleAsync(e => e.PublicId == item.PublicId, cancellationToken)).PosterUrl = "/images/events/parallel.png";
            await other.SaveChangesAsync(cancellationToken);
            Files.Add("/images/events/parallel.png");
            await Files.DeleteFileAsync("/images/events/old.png");
        });

        await Assert.ThrowsAsync<ConflictException>(() => UploadAsync(organizer, item.PublicId, parallelReplace));

        Assert.Equal(["/images/events/parallel.png"], Files.Files);
        Assert.Equal("/images/events/parallel.png", (await FindEventAsync(item.PublicId))!.PosterUrl);
    }

    [Fact]
    public async Task Handle_OtherOrganizersEvent_ThrowsForbiddenAndStoresNothing()
    {
        var owner = await AddUserAsync(Role.Names.Organizer);
        var other = await AddUserAsync(Role.Names.Organizer);
        var item = await AddEventAsync(owner, await AddReferencesAsync());

        await Assert.ThrowsAsync<ForbiddenException>(() => UploadAsync(other, item.PublicId));

        Assert.Empty(Files.Files);
    }

    [Fact]
    public async Task Handle_UnknownEvent_ThrowsNotFound()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);

        await Assert.ThrowsAsync<NotFoundException>(() => UploadAsync(organizer, Guid.NewGuid()));
    }

    [Fact]
    public async Task Handle_Admin_SetsThePosterOfAnyEvent()
    {
        var owner = await AddUserAsync(Role.Names.Organizer);
        var admin = await AddUserAsync(Role.Names.Admin);
        var item = await AddEventAsync(owner, await AddReferencesAsync());

        var result = await UploadAsync(admin, item.PublicId);

        Assert.Equal(result.PosterUrl, (await FindEventAsync(item.PublicId))!.PosterUrl);
    }

    private async Task<EventPosterDto> UploadAsync(AppUser user, Guid eventPublicId, params BeforeSaveInterceptor[] interceptors)
    {
        await using var context = Database.CreateContext(interceptors);
        var handler = new UploadEventPosterCommandHandler(context, SignedIn(user), Files, UploadOptions);
        var command = new UploadEventPosterCommand { PublicId = eventPublicId, File = TestImages.File("poster.png", TestImages.Png) };
        return await handler.Handle(command, CancellationToken.None);
    }
}
