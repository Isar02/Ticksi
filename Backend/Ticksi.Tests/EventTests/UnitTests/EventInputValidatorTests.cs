using Microsoft.Extensions.Time.Testing;
using Ticksi.Application.Features.Events.Commands.CreateEvent;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.EventTests.UnitTests;

public class EventInputValidatorTests
{
    private static readonly DateTimeOffset Today = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly CreateEventCommandValidator _validator = new(EventClocks.In("Europe/Sarajevo", new FakeTimeProvider(Today)));

    [Fact]
    public async Task Validate_CompleteInput_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_MissingFieldsAndPastDate_ReportsEachField()
    {
        var command = new CreateEventCommand { Date = Today.UtcDateTime.AddMinutes(-1) };

        var result = await _validator.ValidateAsync(command);

        Assert.Equal(
            ["CategoryId", "Contact", "Date", "Description", "EventTypeId", "LocationId", "Name", "OrganizerCompanyId", "TicketTypes"],
            result.Errors.Select(e => e.PropertyName).Distinct().Order());
    }

    [Fact]
    public async Task Validate_TimeEarlierTodayInTheEventTimeZone_IsRejectedThoughLaterInUtc()
    {
        var command = ValidCommand();
        command.Date = new DateTime(2026, 10, 5, 13, 30, 0);

        var result = await _validator.ValidateAsync(command);

        Assert.Equal("Date", Assert.Single(result.Errors).PropertyName);
    }

    [Fact]
    public async Task Validate_TicketTypeNamesDifferingOnlyInCase_AreRejected()
    {
        var command = ValidCommand();
        command.TicketTypes[1].Name = " STANDARD ";

        var result = await _validator.ValidateAsync(command);

        Assert.Equal("TicketTypes", Assert.Single(result.Errors).PropertyName);
    }

    [Fact]
    public async Task Validate_NullTicketType_IsRejected()
    {
        var command = ValidCommand();
        command.TicketTypes[1] = null!;

        var result = await _validator.ValidateAsync(command);

        Assert.Equal("TicketTypes[1]", Assert.Single(result.Errors).PropertyName);
    }

    [Fact]
    public async Task Validate_InvalidTicketType_ReportsNamePriceAndQuantity()
    {
        var command = ValidCommand();
        command.TicketTypes[1].Name = "";
        command.TicketTypes[1].Price = 10.555m;
        command.TicketTypes[0].Price = -1m;
        command.TicketTypes[0].Quantity = 0;

        var result = await _validator.ValidateAsync(command);

        Assert.Equal(
            ["TicketTypes[0].Price", "TicketTypes[0].Quantity", "TicketTypes[1].Name", "TicketTypes[1].Price"],
            result.Errors.Select(e => e.PropertyName).Order());
    }

    private static CreateEventCommand ValidCommand() => new()
    {
        Name = "Winter Gala",
        Description = "An evening of music.",
        Date = Today.UtcDateTime.AddDays(30),
        Contact = "gala@ticksi.com",
        CategoryId = Guid.NewGuid(),
        EventTypeId = Guid.NewGuid(),
        LocationId = Guid.NewGuid(),
        OrganizerCompanyId = Guid.NewGuid(),
        TicketTypes =
        [
            new() { Name = "Standard", Price = 30m, Quantity = 800 },
            new() { Name = "VIP", Price = 75.50m, Quantity = 200 }
        ]
    };
}
