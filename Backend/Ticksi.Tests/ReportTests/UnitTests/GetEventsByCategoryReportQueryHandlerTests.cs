using Microsoft.Extensions.Time.Testing;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Reports.Models;
using Ticksi.Application.Features.Reports.Queries.GetEventsByCategoryReport;
using Ticksi.Application.Interfaces;
using Ticksi.Tests.EventTests.UnitTests;

namespace Ticksi.Tests.ReportTests.UnitTests;

public class GetEventsByCategoryReportQueryHandlerTests : EventHandlerTestBase
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 5, 20, 15, 0, TimeSpan.Zero));
    private readonly CapturingRenderer _renderer = new();

    [Fact]
    public async Task Handle_WithRange_ListsTheCategoryEventsOnWholeDaysByDate()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var music = await AddReferencesAsync();
        var other = await AddReferencesAsync();
        await AddEventAsync(organizer, music, name: "Late", date: new DateTime(2026, 10, 31, 23, 30, 0));
        await AddEventAsync(organizer, music, name: "Before", date: new DateTime(2026, 9, 30, 23, 59, 0));
        await AddEventAsync(organizer, music, name: "Early", date: new DateTime(2026, 10, 1, 0, 0, 0));
        await AddEventAsync(organizer, music, name: "After", date: new DateTime(2026, 11, 1, 0, 0, 0));
        await AddEventAsync(organizer, other, name: "Elsewhere", date: new DateTime(2026, 10, 15, 20, 0, 0));

        var pdf = await HandleAsync(Query(music, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)));

        var report = _renderer.Report!;
        Assert.Equal(CapturingRenderer.Output, pdf);
        Assert.Equal(["Early", "Late"], report.Events.Select(e => e.Name));
        Assert.Equal((new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)), (report.DateFrom, report.DateTo));
        var first = report.Events[0];
        Assert.Equal(("Music", "Zetra", "Sarajevo", 20m), (report.CategoryName, first.LocationName, first.City, first.LowestPrice!.Value));
    }

    [Fact]
    public async Task Handle_WithoutRange_ListsEveryEventOfTheCategory()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var music = await AddReferencesAsync();
        await AddEventAsync(organizer, music, name: "Next year");
        await AddEventAsync(organizer, music, name: "Last year", date: new DateTime(2025, 6, 1, 20, 0, 0));

        await HandleAsync(Query(music, null, null));

        Assert.Equal(["Last year", "Next year"], _renderer.Report!.Events.Select(e => e.Name));
    }

    [Fact]
    public async Task Handle_GenerationTime_IsTheUtcClockInLocalTime()
    {
        var music = await AddReferencesAsync();
        _clock.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Sarajevo"));

        await HandleAsync(Query(music, null, null));

        var generatedAt = _renderer.Report!.GeneratedAt;
        Assert.Equal((new DateTime(2026, 10, 5, 22, 15, 0), TimeSpan.FromHours(2)), (generatedAt.DateTime, generatedAt.Offset));
    }

    [Fact]
    public async Task Handle_UnknownCategory_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            HandleAsync(new GetEventsByCategoryReportQuery { CategoryPublicId = Guid.NewGuid() }));
        Assert.Null(_renderer.Report);
    }

    private static GetEventsByCategoryReportQuery Query(References references, DateOnly? from, DateOnly? to) =>
        new() { CategoryPublicId = references.CategoryId, DateFrom = from, DateTo = to };

    private async Task<byte[]> HandleAsync(GetEventsByCategoryReportQuery query)
    {
        await using var context = Database.CreateContext();
        return await new GetEventsByCategoryReportQueryHandler(context, _renderer, _clock).Handle(query, CancellationToken.None);
    }

    private sealed class CapturingRenderer : IReportRenderer
    {
        public static readonly byte[] Output = [1, 2, 3];

        public EventsByCategoryReport? Report { get; private set; }

        public byte[] RenderEventsByCategory(EventsByCategoryReport report)
        {
            Report = report;
            return Output;
        }
    }
}
