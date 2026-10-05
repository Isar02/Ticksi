using Ticksi.Application.Features.Reports.Models;

namespace Ticksi.Application.Interfaces;

public interface IReportRenderer
{
    byte[] RenderEventsByCategory(EventsByCategoryReport report);
}
