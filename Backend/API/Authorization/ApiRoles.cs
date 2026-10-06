using Ticksi.Domain.Entities;

namespace API.Authorization;

public static class ApiRoles
{
    public const string EventManagers = $"{Role.Names.Admin},{Role.Names.Organizer}";
}
