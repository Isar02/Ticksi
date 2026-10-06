using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<AppUser> AppUsers { get; }
        DbSet<Role> Roles { get; }
        DbSet<EventCategory> EventCategories { get; }
        DbSet<Event> Events { get; }
        DbSet<EventType> EventTypes { get; }
        DbSet<Location> Locations { get; }
        DbSet<OrganizerCompany> OrganizerCompanies { get; }
        DbSet<TicketType> TicketTypes { get; }
        DbSet<Ticket> Tickets { get; }
        DbSet<Order> Orders { get; }
        DbSet<OrderItem> OrderItems { get; }
        DbSet<Cart> Carts { get; }
        DbSet<CartItem> CartItems { get; }
        DbSet<Review> Reviews { get; }
        DbSet<Notification> Notifications { get; }
        DbSet<Favorite> Favorites { get; }
        DbSet<RefreshToken> RefreshTokens { get; }
        DbSet<Payment> Payments { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task<IDbContextTransaction> BeginPasswordChangeAsync(Guid userPublicId, CancellationToken cancellationToken = default);
        Task<IDbContextTransaction> BeginUserAdministrationAsync(CancellationToken cancellationToken = default);
    }
}

