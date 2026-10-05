using MediatR;
using Ticksi.Application.DTOs;

namespace Ticksi.Application.Features.Events.Commands.CreateEvent;

public class CreateEventCommand : EventInput, IRequest<EventReadDto>;
