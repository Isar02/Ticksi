using MediatR;

namespace Ticksi.Application.Features.Profile.Queries.GetProfile;

public record GetProfileQuery : IRequest<ProfileDto>;
