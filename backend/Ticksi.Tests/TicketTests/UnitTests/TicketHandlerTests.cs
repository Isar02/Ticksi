using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Tickets;
using Ticksi.Application.Features.Tickets.Queries.GetMyTickets;
using Ticksi.Application.Features.Tickets.Queries.GetTicketQrCode;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Enums;
using Ticksi.Tests.EventTests.UnitTests;

namespace Ticksi.Tests.TicketTests.UnitTests;

public class TicketHandlerTests : EventHandlerTestBase
{
    private readonly FakeQrCodes _qrCodes = new();

    [Fact]
    public async Task GetMine_ReturnsOnlyTheBuyersTicketsByEventDate()
    {
        var buyer = await AddUserAsync(Role.Names.User);
        var other = await AddUserAsync(Role.Names.User);
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        var later = await AddEventAsync(organizer, references, name: "Winter Gala", date: NextYear.AddMonths(5));
        var sooner = await AddEventAsync(organizer, references, name: "Summer Concert");
        await AddTicketsAsync(buyer, later, "VIP", "LATERVIP0001");
        await AddTicketsAsync(buyer, sooner, "Standard", "SOONSTD00001", "SOONSTD00002");
        await AddTicketsAsync(other, sooner, "Standard", "OTHERSTD0001");

        var tickets = await GetMineAsync(buyer);

        Assert.Equal(["SOONSTD00001", "SOONSTD00002", "LATERVIP0001"], tickets.Select(t => t.Code));
        var first = tickets[0];
        Assert.Equal(("Summer Concert", NextYear, "Standard", "Zetra", "Sarajevo", TicketStatus.Valid),
            (first.EventName, first.EventDate, first.TicketTypeName, first.VenueName, first.VenueCity, first.Status));
        Assert.Equal(sooner.PublicId, first.EventId);
    }

    [Fact]
    public async Task GetMine_WithoutPaidOrders_ReturnsNothing()
    {
        var buyer = await AddUserAsync(Role.Names.User);
        var item = await AddEventAsync(await AddUserAsync(Role.Names.Organizer), await AddReferencesAsync());
        await AddOrderAsync(buyer, item.TicketTypes.First().PublicId, 2, OrderStatus.Pending);

        Assert.Empty(await GetMineAsync(buyer));
    }

    [Fact]
    public async Task GetQrCode_ForOwnTicket_RendersItsCode()
    {
        var buyer = await AddUserAsync(Role.Names.User);
        var item = await AddEventAsync(await AddUserAsync(Role.Names.Organizer), await AddReferencesAsync());
        var ticket = (await AddTicketsAsync(buyer, item, "VIP", "QRCODE000001")).Single();

        var png = await GetQrCodeAsync(buyer, ticket);

        Assert.Equal(["QRCODE000001"], _qrCodes.Rendered);
        Assert.Equal(FakeQrCodes.Image, png);
    }

    [Fact]
    public async Task GetQrCode_ForAnotherUsersOrUnknownTicket_ThrowsNotFound()
    {
        var buyer = await AddUserAsync(Role.Names.User);
        var other = await AddUserAsync(Role.Names.User);
        var item = await AddEventAsync(await AddUserAsync(Role.Names.Organizer), await AddReferencesAsync());
        var ticket = (await AddTicketsAsync(buyer, item, "VIP", "QRCODE000002")).Single();

        await Assert.ThrowsAsync<NotFoundException>(() => GetQrCodeAsync(other, ticket));
        await Assert.ThrowsAsync<NotFoundException>(() => GetQrCodeAsync(buyer, Guid.NewGuid()));
        Assert.Empty(_qrCodes.Rendered);
    }

    private async Task<List<Guid>> AddTicketsAsync(AppUser buyer, Event item, string ticketTypeName, params string[] codes)
    {
        await using var context = Database.CreateContext();
        var ticketType = await context.TicketTypes.SingleAsync(t => t.EventId == item.Id && t.Name == ticketTypeName);
        var tickets = codes.Select(code => new Ticket { Code = code, IssuedAtUtc = NextYear.AddMonths(-1) }).ToList();

        context.Orders.Add(new Order
        {
            AppUserId = buyer.Id,
            Status = OrderStatus.Paid,
            TotalAmount = ticketType.Price * codes.Length,
            Items = [new OrderItem { TicketTypeId = ticketType.Id, Quantity = codes.Length, UnitPrice = ticketType.Price, Tickets = tickets }]
        });
        await context.SaveChangesAsync();
        return tickets.Select(t => t.PublicId).ToList();
    }

    private async Task<List<TicketDto>> GetMineAsync(AppUser user)
    {
        await using var context = Database.CreateContext();
        return await new GetMyTicketsQueryHandler(context, SignedIn(user)).Handle(new GetMyTicketsQuery(), CancellationToken.None);
    }

    private async Task<byte[]> GetQrCodeAsync(AppUser user, Guid ticketId)
    {
        await using var context = Database.CreateContext();
        return await new GetTicketQrCodeQueryHandler(context, SignedIn(user), _qrCodes)
            .Handle(new GetTicketQrCodeQuery(ticketId), CancellationToken.None);
    }

    private sealed class FakeQrCodes : IQrCodeGenerator
    {
        public static readonly byte[] Image = [1, 2, 3];
        public List<string> Rendered { get; } = [];

        public byte[] RenderPng(string content)
        {
            Rendered.Add(content);
            return Image;
        }
    }
}
