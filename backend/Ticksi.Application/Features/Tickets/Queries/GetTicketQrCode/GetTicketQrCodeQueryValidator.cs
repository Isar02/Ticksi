using FluentValidation;

namespace Ticksi.Application.Features.Tickets.Queries.GetTicketQrCode;

public class GetTicketQrCodeQueryValidator : AbstractValidator<GetTicketQrCodeQuery>
{
    public GetTicketQrCodeQueryValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty().WithMessage("Ticket is required.");
    }
}
