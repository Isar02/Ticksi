using Microsoft.EntityFrameworkCore;

namespace Ticksi.Application.Common;

public static class PagingExtensions
{
    public const int MaxPageSize = 50;

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (long)(page - 1) * pageSize;

        var items = skip >= totalCount
            ? new List<T>()
            : await query.Skip((int)skip).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
