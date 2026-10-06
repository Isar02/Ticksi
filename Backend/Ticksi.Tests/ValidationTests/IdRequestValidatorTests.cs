using FluentValidation;
using Ticksi.Application.Features.Events.Queries.GetEventForEdit;
using Ticksi.Application.Features.Events.Queries.GetEventTicketTypes;
using Ticksi.Application.Features.Orders.Queries.GetOrderById;
using Ticksi.Application.Features.Payments.Commands.ConfirmPayment;
using Ticksi.Application.Features.Payments.Commands.StartPayment;
using Ticksi.Application.Features.Tickets.Queries.GetTicketQrCode;
using Ticksi.Application.Features.Users.Queries.GetUserById;

namespace Ticksi.Tests.ValidationTests;

public class IdRequestValidatorTests
{
    public static TheoryData<IValidator, Func<Guid, object>, string> Requests => new()
    {
        { new GetEventForEditQueryValidator(), id => new GetEventForEditQuery(id), "Event is required." },
        { new GetEventTicketTypesQueryValidator(), id => new GetEventTicketTypesQuery(id), "Event is required." },
        { new GetOrderByIdQueryValidator(), id => new GetOrderByIdQuery(id), "Order is required." },
        { new StartPaymentCommandValidator(), id => new StartPaymentCommand(id), "Order is required." },
        { new ConfirmPaymentCommandValidator(), id => new ConfirmPaymentCommand(id), "Order is required." },
        { new GetTicketQrCodeQueryValidator(), id => new GetTicketQrCodeQuery(id), "Ticket is required." },
        { new GetUserByIdQueryValidator(), id => new GetUserByIdQuery(id), "User is required." }
    };

    [Theory]
    [MemberData(nameof(Requests))]
    public async Task Validate_EmptyId_FailsWithItsMessageAndAnIdPasses(IValidator validator, Func<Guid, object> request, string message)
    {
        var empty = await validator.ValidateAsync(new ValidationContext<object>(request(Guid.Empty)));
        var filled = await validator.ValidateAsync(new ValidationContext<object>(request(Guid.NewGuid())));

        Assert.Equal(message, Assert.Single(empty.Errors).ErrorMessage);
        Assert.True(filled.IsValid);
    }
}
