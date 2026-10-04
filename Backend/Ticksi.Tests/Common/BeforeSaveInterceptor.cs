using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Ticksi.Tests.Common;

// Runs a change from another request just before the first few saves, to provoke concurrency conflicts.
public sealed class BeforeSaveInterceptor(int times, Func<CancellationToken, Task> otherRequest) : SaveChangesInterceptor
{
    private int _remaining = times;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (_remaining > 0)
        {
            _remaining--;
            await otherRequest(cancellationToken);
        }

        return result;
    }
}
