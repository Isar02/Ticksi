using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Events.Commands;

internal static class EventWriter
{
    public static async Task SaveAsync(
        IAppDbContext context,
        Event item,
        EventInput input,
        CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();

        var categoryId = await IdOfAsync(context.EventCategories, input.CategoryId, cancellationToken);
        var eventTypeId = await IdOfAsync(context.EventTypes, input.EventTypeId, cancellationToken);
        var companyId = await IdOfAsync(context.OrganizerCompanies, input.OrganizerCompanyId, cancellationToken);
        var venue = await context.Locations
            .Where(l => l.PublicId == input.LocationId)
            .Select(l => new { l.Id, l.Capacity })
            .FirstOrDefaultAsync(cancellationToken);

        if (categoryId is null)
            failures.Add(new(nameof(EventInput.CategoryId), "The selected category does not exist."));
        if (eventTypeId is null)
            failures.Add(new(nameof(EventInput.EventTypeId), "The selected event type does not exist."));
        if (companyId is null)
            failures.Add(new(nameof(EventInput.OrganizerCompanyId), "The selected organizer company does not exist."));
        if (venue is null)
            failures.Add(new(nameof(EventInput.LocationId), "The selected venue does not exist."));
        else if (input.TicketTypes.Sum(t => (long)t.Quantity) > venue.Capacity)
            failures.Add(new(nameof(EventInput.TicketTypes), $"The venue holds {venue.Capacity} people, so the ticket quantities cannot add up to more."));

        var existing = item.TicketTypes.ToDictionary(t => t.PublicId);
        failures.AddRange(CheckTicketTypes(input.TicketTypes, existing));

        if (failures.Count > 0)
            throw new ValidationException(failures);

        var kept = input.TicketTypes.Where(t => t.PublicId.HasValue).Select(t => t.PublicId!.Value).ToHashSet();
        var removed = existing.Values.Where(t => !kept.Contains(t.PublicId)).ToList();
        await EnsureNotOrderedAsync(context, removed, cancellationToken);

        item.Name = input.Name.Trim();
        item.Description = input.Description.Trim();
        item.Date = input.Date;
        item.Contact = input.Contact.Trim();
        item.EventCategoryId = categoryId!.Value;
        item.EventTypeId = eventTypeId!.Value;
        item.OrganizerCompanyId = companyId!.Value;
        item.LocationId = venue!.Id;

        context.TicketTypes.RemoveRange(removed);

        var renamed = input.TicketTypes
            .Where(t => t.PublicId.HasValue && existing[t.PublicId.Value].Name != t.Name.Trim())
            .Select(t => existing[t.PublicId!.Value])
            .ToList();

        await using var transaction = renamed.Count > 0 ? await context.BeginTransactionAsync(cancellationToken) : null;

        if (renamed.Count > 0)
            await FreeOldNamesAsync(context, renamed, cancellationToken);

        foreach (var ticketInput in input.TicketTypes)
        {
            var ticketType = ticketInput.PublicId is { } publicId ? existing[publicId] : new TicketType();
            ticketType.Name = ticketInput.Name.Trim();
            ticketType.Price = ticketInput.Price;
            ticketType.Quantity = ticketInput.Quantity;

            if (ticketInput.PublicId is null)
                item.TicketTypes.Add(ticketType);
        }

        await context.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    // Names are unique per event, so a swap such as Standard <-> VIP needs the old names released first.
    private static async Task FreeOldNamesAsync(
        IAppDbContext context,
        List<TicketType> renamed,
        CancellationToken cancellationToken)
    {
        foreach (var ticketType in renamed)
            ticketType.Name = Guid.NewGuid().ToString("N");

        await context.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<ValidationFailure> CheckTicketTypes(
        List<TicketTypeInput> inputs,
        Dictionary<Guid, TicketType> existing)
    {
        var seen = new HashSet<Guid>();

        for (var i = 0; i < inputs.Count; i++)
        {
            if (inputs[i].PublicId is not { } publicId)
                continue;

            var path = $"{nameof(EventInput.TicketTypes)}[{i}]";

            if (!existing.TryGetValue(publicId, out var ticketType) || !seen.Add(publicId))
                yield return new($"{path}.{nameof(TicketTypeInput.PublicId)}", "This ticket type does not belong to the event.");
            else if (inputs[i].Quantity < ticketType.QuantityReserved)
                yield return new($"{path}.{nameof(TicketTypeInput.Quantity)}",
                    $"{ticketType.QuantityReserved} tickets of this type are already sold or reserved.");
        }
    }

    private static async Task EnsureNotOrderedAsync(
        IAppDbContext context,
        List<TicketType> removed,
        CancellationToken cancellationToken)
    {
        if (removed.Count == 0)
            return;

        var removedIds = removed.Select(t => t.Id).ToList();
        var ordered = await context.OrderItems
            .Where(i => removedIds.Contains(i.TicketTypeId))
            .Select(i => i.TicketType!.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (ordered is not null)
            throw new ConflictException($"Tickets of type \"{ordered}\" have been ordered, so it cannot be removed.");
    }

    private static Task<int?> IdOfAsync<T>(IQueryable<T> entities, Guid publicId, CancellationToken cancellationToken)
        where T : BaseEntity =>
        entities
            .Where(e => e.PublicId == publicId)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync(cancellationToken);
}
