using FluentValidation;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Events.Commands.CreateEvent;

namespace Ticksi.Tests.EventTests.UnitTests;

public class CreateEventCommandHandlerTests : EventHandlerTestBase
{
    [Fact]
    public async Task Handle_Organizer_SavesTheEventWithItsTicketTypesUnderTheOrganizer()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();

        var dto = await CreateAsync(organizer, Input<CreateEventCommand>(references));

        var saved = await FindEventAsync(dto.PublicId);
        Assert.NotNull(saved);
        Assert.Equal(organizer.Id, saved.AppUserId);
        Assert.Equal("Winter Gala", saved.Name);
        Assert.Equal(["Standard", "VIP"], saved.TicketTypes.Select(t => t.Name).Order());
        Assert.Equal(30m, dto.LowestPrice);
        Assert.Equal(1000, dto.AvailableTickets);
    }

    [Fact]
    public async Task Handle_UnknownReferences_RejectsEachField()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var command = Input<CreateEventCommand>(new References(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        var error = await Assert.ThrowsAsync<ValidationException>(() => CreateAsync(organizer, command));

        Assert.Equal(
            ["CategoryId", "EventTypeId", "LocationId", "OrganizerCompanyId"],
            error.Errors.Select(e => e.PropertyName).Order());
    }

    [Fact]
    public async Task Handle_TicketsAboveVenueCapacity_RejectsTheTicketTypes()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync(capacity: 999);

        var error = await Assert.ThrowsAsync<ValidationException>(
            () => CreateAsync(organizer, Input<CreateEventCommand>(references)));

        Assert.Equal("TicketTypes", Assert.Single(error.Errors).PropertyName);
    }

    [Fact]
    public async Task Handle_TicketTypeIdOnANewEvent_IsRejected()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var command = Input<CreateEventCommand>(await AddReferencesAsync());
        command.TicketTypes[1].PublicId = Guid.NewGuid();

        var error = await Assert.ThrowsAsync<ValidationException>(() => CreateAsync(organizer, command));

        Assert.Equal("TicketTypes[1].PublicId", Assert.Single(error.Errors).PropertyName);
    }

    [Fact]
    public async Task Handle_UserRole_ThrowsForbiddenAndSavesNothing()
    {
        var user = await AddUserAsync(Role.Names.User);
        var references = await AddReferencesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => CreateAsync(user, Input<CreateEventCommand>(references)));

        await using var context = Database.CreateContext();
        Assert.False(await context.Events.AnyAsync());
    }

    [Fact]
    public async Task Handle_DeactivatedOrganizer_ThrowsUnauthorized()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer, isActive: false);
        var references = await AddReferencesAsync();

        await Assert.ThrowsAsync<UnauthorizedException>(() => CreateAsync(organizer, Input<CreateEventCommand>(references)));
    }

    private async Task<EventReadDto> CreateAsync(AppUser user, CreateEventCommand command)
    {
        await using var context = Database.CreateContext();
        var handler = new CreateEventCommandHandler(context, SignedIn(user));
        return await handler.Handle(command, CancellationToken.None);
    }
}
