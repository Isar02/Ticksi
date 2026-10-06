using Ticksi.Application.Interfaces;

namespace Ticksi.Tests.Common;

public sealed class FakeCurrentUser(Guid? publicId) : ICurrentUser
{
    public Guid? PublicId { get; } = publicId;
}
