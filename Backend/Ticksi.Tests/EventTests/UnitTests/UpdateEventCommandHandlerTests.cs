using FluentValidation;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Events.Commands;
using Ticksi.Application.Features.Events.Commands.UpdateEvent;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.EventTests.UnitTests;

public class UpdateEventCommandHandlerTests : EventHandlerTestBase
{
    [Fact]
    public async Task Handle_OwnEvent_UpdatesTheFieldsAndSyncsTheTicketTypes()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        var item = await AddEventAsync(organizer, references);
        var standard = item.TicketTypes.Single(t => t.Name == "Standard");

        var command = CommandFor(item, references);
        command.TicketTypes =
        [
            new() { PublicId = standard.PublicId, Name = "Regular", Price = 25m, Quantity = 600 },
            new() { Name = "Student", Price = 10m, Quantity = 50 }
        ];
        await UpdateAsync(organizer, command);

        var saved = await FindEventAsync(item.PublicId);
        Assert.Equal("Winter Gala", saved!.Name);
        Assert.Equal(organizer.Id, saved.AppUserId);
        Assert.Equal(["Regular", "Student"], saved.TicketTypes.Select(t => t.Name).Order());
        var regular = saved.TicketTypes.Single(t => t.Name == "Regular");
        Assert.Equal(standard.PublicId, regular.PublicId);
        Assert.Equal((25m, 600), (regular.Price, regular.Quantity));
    }

    [Fact]
    public async Task Handle_OtherOrganizersEvent_ThrowsForbiddenAndChangesNothing()
    {
        var owner = await AddUserAsync(Role.Names.Organizer);
        var other = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        var item = await AddEventAsync(owner, references);

        await Assert.ThrowsAsync<ForbiddenException>(() => UpdateAsync(other, CommandFor(item, references)));

        Assert.Equal("Summer Concert", (await FindEventAsync(item.PublicId))!.Name);
    }

    [Fact]
    public async Task Handle_Admin_UpdatesAnyEventAndKeepsItsOwner()
    {
        var owner = await AddUserAsync(Role.Names.Organizer);
        var admin = await AddUserAsync(Role.Names.Admin);
        var references = await AddReferencesAsync();
        var item = await AddEventAsync(owner, references);

        await UpdateAsync(admin, CommandFor(item, references));

        var saved = await FindEventAsync(item.PublicId);
        Assert.Equal("Winter Gala", saved!.Name);
        Assert.Equal(owner.Id, saved.AppUserId);
    }

    [Fact]
    public async Task Handle_QuantityBelowSoldTickets_RejectsTheQuantity()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        var item = await AddEventAsync(organizer, references, reserved: 300);

        var command = CommandFor(item, references);
        command.TicketTypes[0].Quantity = 299;

        var error = await Assert.ThrowsAsync<ValidationException>(() => UpdateAsync(organizer, command));

        Assert.Equal("TicketTypes[0].Quantity", Assert.Single(error.Errors).PropertyName);
    }

    [Fact]
    public async Task Handle_RemovingAnOrderedTicketType_ThrowsConflictAndKeepsIt()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var buyer = await AddUserAsync(Role.Names.User);
        var references = await AddReferencesAsync();
        var item = await AddEventAsync(organizer, references);
        var vip = item.TicketTypes.Single(t => t.Name == "VIP");
        await AddOrderAsync(buyer, vip.PublicId, 2);

        var command = CommandFor(item, references);
        command.TicketTypes.RemoveAll(t => t.PublicId == vip.PublicId);

        await Assert.ThrowsAsync<ConflictException>(() => UpdateAsync(organizer, command));

        Assert.Contains((await FindEventAsync(item.PublicId))!.TicketTypes, t => t.PublicId == vip.PublicId);
    }

    [Fact]
    public async Task Handle_TicketTypeOfAnotherEvent_IsRejected()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        var item = await AddEventAsync(organizer, references);
        var otherEvent = await AddEventAsync(organizer, references);

        var command = CommandFor(item, references);
        command.TicketTypes[1].PublicId = otherEvent.TicketTypes.First().PublicId;

        var error = await Assert.ThrowsAsync<ValidationException>(() => UpdateAsync(organizer, command));

        Assert.Equal("TicketTypes[1].PublicId", Assert.Single(error.Errors).PropertyName);
    }

    [Fact]
    public async Task Handle_TicketsSoldWhileEditing_ThrowsConflict()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        var item = await AddEventAsync(organizer, references);
        var standardId = item.TicketTypes.Single(t => t.Name == "Standard").PublicId;

        var parallelPurchase = new BeforeSaveInterceptor(1, async cancellationToken =>
        {
            await using var other = Database.CreateContext();
            var standard = await other.TicketTypes.SingleAsync(t => t.PublicId == standardId, cancellationToken);
            standard.QuantityReserved += 2;
            await other.SaveChangesAsync(cancellationToken);
        });

        var command = CommandFor(item, references);
        command.TicketTypes.Single(t => t.PublicId == standardId).Price = 22m;

        await Assert.ThrowsAsync<ConflictException>(() => UpdateAsync(organizer, command, parallelPurchase));
    }

    [Fact]
    public async Task Handle_UnknownEvent_ThrowsNotFound()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var command = Input<UpdateEventCommand>(await AddReferencesAsync());
        command.PublicId = Guid.NewGuid();

        await Assert.ThrowsAsync<NotFoundException>(() => UpdateAsync(organizer, command));
    }

    private static UpdateEventCommand CommandFor(Event item, References references)
    {
        var command = Input<UpdateEventCommand>(references);
        command.PublicId = item.PublicId;
        command.TicketTypes = item.TicketTypes
            .Select(t => new TicketTypeInput { PublicId = t.PublicId, Name = t.Name, Price = t.Price, Quantity = t.Quantity })
            .ToList();
        return command;
    }

    private async Task UpdateAsync(AppUser user, UpdateEventCommand command, params IInterceptor[] interceptors)
    {
        await using var context = Database.CreateContext(interceptors);
        var handler = new UpdateEventCommandHandler(context, SignedIn(user));
        await handler.Handle(command, CancellationToken.None);
    }
}
