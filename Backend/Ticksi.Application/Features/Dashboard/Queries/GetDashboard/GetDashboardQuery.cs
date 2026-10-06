using MediatR;

namespace Ticksi.Application.Features.Dashboard.Queries.GetDashboard;

public record GetDashboardQuery : IRequest<DashboardDto>;
