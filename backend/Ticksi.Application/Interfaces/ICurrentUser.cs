namespace Ticksi.Application.Interfaces;

public interface ICurrentUser
{
    Guid? PublicId { get; }
}
