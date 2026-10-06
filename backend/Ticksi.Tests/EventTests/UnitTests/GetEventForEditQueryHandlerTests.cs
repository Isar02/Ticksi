using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Events.Queries.GetEventForEdit;

namespace Ticksi.Tests.EventTests.UnitTests;

public class GetEventForEditQueryHandlerTests : EventHandlerTestBase
{
    [Fact]
    public async Task Handle_OwnEvent_ReturnsTheFieldsReferencesAndTicketTypes()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        var item = await AddEventAsync(organizer, references, reserved: 12);

        var dto = await QueryAsync(organizer, item.PublicId);

        Assert.Equal(("Summer Concert", NextYear), (dto.Name, dto.Date));
        Assert.Equal(references, new References(dto.CategoryId, dto.EventTypeId, dto.LocationId, dto.OrganizerCompanyId));
        Assert.Equal(
            [("Standard", 20m, 500, 12), ("VIP", 50m, 100, 0)],
            dto.TicketTypes.Select(t => (t.Name, t.Price, t.Quantity, t.QuantityReserved)));
        Assert.Equal(
            item.TicketTypes.OrderBy(t => t.Id).Select(t => t.PublicId),
            dto.TicketTypes.Select(t => t.PublicId));
    }

    [Fact]
    public async Task Handle_InactiveCategory_StillReturnsItsName()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        var item = await AddEventAsync(organizer, references);
        await using (var context = Database.CreateContext())
        {
            var category = await context.EventCategories.SingleAsync(c => c.PublicId == references.CategoryId);
            category.IsActive = false;
            await context.SaveChangesAsync();
        }

        var dto = await QueryAsync(organizer, item.PublicId);

        Assert.Equal((references.CategoryId, "Music"), (dto.CategoryId, dto.CategoryName));
    }

    [Fact]
    public async Task Handle_OtherOrganizersEvent_ThrowsForbidden()
    {
        var owner = await AddUserAsync(Role.Names.Organizer);
        var other = await AddUserAsync(Role.Names.Organizer);
        var item = await AddEventAsync(owner, await AddReferencesAsync());

        await Assert.ThrowsAsync<ForbiddenException>(() => QueryAsync(other, item.PublicId));
    }

    [Fact]
    public async Task Handle_Admin_LoadsAnyEvent()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        var item = await AddEventAsync(await AddUserAsync(Role.Names.Organizer), await AddReferencesAsync());

        var dto = await QueryAsync(admin, item.PublicId);

        Assert.Equal(item.PublicId, dto.PublicId);
    }

    [Fact]
    public async Task Handle_UnknownEvent_ThrowsNotFound()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);

        await Assert.ThrowsAsync<NotFoundException>(() => QueryAsync(organizer, Guid.NewGuid()));
    }

    private async Task<EventForEditDto> QueryAsync(AppUser user, Guid eventPublicId)
    {
        await using var context = Database.CreateContext();
        var handler = new GetEventForEditQueryHandler(context, SignedIn(user));
        return await handler.Handle(new GetEventForEditQuery(eventPublicId), CancellationToken.None);
    }
}
