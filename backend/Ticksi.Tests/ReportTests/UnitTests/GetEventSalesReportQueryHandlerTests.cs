using Microsoft.Extensions.Time.Testing;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Reports.Models;
using Ticksi.Application.Features.Reports.Queries.GetEventSalesReport;
using Ticksi.Domain.Enums;
using Ticksi.Tests.EventTests.UnitTests;

namespace Ticksi.Tests.ReportTests.UnitTests;

public class GetEventSalesReportQueryHandlerTests : EventHandlerTestBase
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 6, 9, 0, 0, TimeSpan.Zero));
    private readonly CapturingReportRenderer _renderer = new();

    public GetEventSalesReportQueryHandlerTests()
    {
        _clock.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Sarajevo"));
    }

    [Fact]
    public async Task Handle_OwnEvent_SumsOnlyPaidOrdersPerTicketTypeAndLocalDay()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var buyer = await AddUserAsync(Role.Names.User);
        var references = await AddReferencesAsync();
        var concert = await AddEventAsync(organizer, references);
        var other = await AddEventAsync(organizer, references, name: "Winter Gala");
        await AddSaleAsync(buyer, concert, "Standard", 2, Utc(10, 1, 22, 30), unitPrice: 18m);
        await AddSaleAsync(buyer, concert, "Standard", 1, Utc(10, 2, 9, 0));
        await AddSaleAsync(buyer, concert, "VIP", 3, Utc(10, 3, 15, 0));
        await AddSaleAsync(buyer, concert, "VIP", 5, null, OrderStatus.Pending);
        await AddSaleAsync(buyer, concert, "VIP", 4, Utc(10, 3, 15, 0), OrderStatus.Cancelled);
        await AddSaleAsync(buyer, other, "Standard", 6, Utc(10, 2, 9, 0));

        var pdf = await HandleAsync(organizer, Query(concert));

        var report = _renderer.EventSales!;
        Assert.Equal(CapturingReportRenderer.Output, pdf);
        Assert.Equal([new TicketTypeSales("Standard", 3, 56m), new TicketTypeSales("VIP", 3, 150m)], report.TicketTypes);
        Assert.Equal([new DailySales(new DateOnly(2026, 10, 2), 3, 56m), new DailySales(new DateOnly(2026, 10, 3), 3, 150m)], report.Days);
        Assert.Equal((6, 206m), (report.TicketsSold, report.Revenue));
        Assert.Equal(("Summer Concert", NextYear, "Zetra", "Sarajevo"), (report.EventName, report.EventDate, report.VenueName, report.VenueCity));
    }

    [Fact]
    public async Task Handle_WithRange_CountsSalesOnWholeLocalDays()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var buyer = await AddUserAsync(Role.Names.User);
        var concert = await AddEventAsync(organizer, await AddReferencesAsync());
        await AddSaleAsync(buyer, concert, "Standard", 1, Utc(10, 1, 21, 59));
        await AddSaleAsync(buyer, concert, "Standard", 2, Utc(10, 1, 22, 0));
        await AddSaleAsync(buyer, concert, "Standard", 4, Utc(10, 2, 21, 59));
        await AddSaleAsync(buyer, concert, "Standard", 8, Utc(10, 2, 22, 0));

        var day = new DateOnly(2026, 10, 2);
        await HandleAsync(organizer, Query(concert, day, day));

        var report = _renderer.EventSales!;
        Assert.Equal([new DailySales(day, 6, 120m)], report.Days);
        Assert.Equal((day, day), (report.DateFrom, report.DateTo));
    }

    [Theory]
    [InlineData("2026-10-02")]
    [InlineData("9999-12-31")]
    public async Task Handle_EndDate_IncludesTheLastInstantOfTheLocalDay(string endDate)
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var buyer = await AddUserAsync(Role.Names.User);
        var concert = await AddEventAsync(organizer, await AddReferencesAsync());
        var day = DateOnly.Parse(endDate);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MaxValue), _clock.LocalTimeZone);
        await AddSaleAsync(buyer, concert, "Standard", 2, endUtc.AddTicks(-1));
        await AddSaleAsync(buyer, concert, "Standard", 4, endUtc);
        await AddSaleAsync(buyer, concert, "Standard", 8, endUtc.AddTicks(1));

        await HandleAsync(organizer, Query(concert, null, day));

        var report = _renderer.EventSales!;
        Assert.Equal([new DailySales(day, 6, 120m)], report.Days);
        Assert.Equal((6, 120m), (report.TicketsSold, report.Revenue));
        Assert.Equal(day, report.DateTo);
    }

    [Fact]
    public async Task Handle_WithoutSales_ListsEveryTicketTypeAtZero()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var concert = await AddEventAsync(organizer, await AddReferencesAsync());

        await HandleAsync(organizer, Query(concert));

        var report = _renderer.EventSales!;
        Assert.Equal([new TicketTypeSales("Standard", 0, 0m), new TicketTypeSales("VIP", 0, 0m)], report.TicketTypes);
        Assert.Empty(report.Days);
    }

    [Fact]
    public async Task Handle_AsAdmin_ReportsAnyEvent()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var concert = await AddEventAsync(organizer, await AddReferencesAsync());
        await AddSaleAsync(await AddUserAsync(Role.Names.User), concert, "VIP", 2, Utc(10, 2, 9, 0));

        await HandleAsync(await AddUserAsync(Role.Names.Admin), Query(concert));

        Assert.Equal(2, _renderer.EventSales!.TicketsSold);
    }

    [Fact]
    public async Task Handle_AnotherOrganizersEvent_ThrowsForbidden()
    {
        var concert = await AddEventAsync(await AddUserAsync(Role.Names.Organizer), await AddReferencesAsync());
        var otherOrganizer = await AddUserAsync(Role.Names.Organizer);

        await Assert.ThrowsAsync<ForbiddenException>(() => HandleAsync(otherOrganizer, Query(concert)));
        Assert.Null(_renderer.EventSales);
    }

    [Fact]
    public async Task Handle_UnknownEvent_ThrowsNotFound()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            HandleAsync(organizer, new GetEventSalesReportQuery { EventPublicId = Guid.NewGuid() }));
        Assert.Null(_renderer.EventSales);
    }

    private static DateTime Utc(int month, int day, int hour, int minute) =>
        new(2026, month, day, hour, minute, 0, DateTimeKind.Utc);

    private static GetEventSalesReportQuery Query(Event item, DateOnly? from = null, DateOnly? to = null) =>
        new() { EventPublicId = item.PublicId, DateFrom = from, DateTo = to };

    private async Task AddSaleAsync(
        AppUser buyer,
        Event item,
        string ticketTypeName,
        int quantity,
        DateTime? paidAtUtc,
        OrderStatus status = OrderStatus.Paid,
        decimal? unitPrice = null)
    {
        await using var context = Database.CreateContext();
        var ticketType = await context.TicketTypes.SingleAsync(t => t.EventId == item.Id && t.Name == ticketTypeName);
        var price = unitPrice ?? ticketType.Price;

        context.Orders.Add(new Order
        {
            AppUserId = buyer.Id,
            Status = status,
            TotalAmount = price * quantity,
            PaidAtUtc = paidAtUtc,
            Items = [new OrderItem { TicketTypeId = ticketType.Id, Quantity = quantity, UnitPrice = price }]
        });
        await context.SaveChangesAsync();
    }

    private async Task<byte[]> HandleAsync(AppUser user, GetEventSalesReportQuery query)
    {
        await using var context = Database.CreateContext();
        return await new GetEventSalesReportQueryHandler(context, SignedIn(user), _renderer, _clock)
            .Handle(query, CancellationToken.None);
    }
}
