using Microsoft.EntityFrameworkCore;
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
        DbSet<Ticket> Tickets { get; }
        DbSet<Order> Orders { get; }
        DbSet<OrderItem> OrderItems { get; }
        DbSet<Cart> Carts { get; }
        DbSet<CartItem> CartItems { get; }
        DbSet<Review> Reviews { get; }
        DbSet<Notification> Notifications { get; }
        DbSet<PromoCode> PromoCodes { get; }
        DbSet<Transaction> Transactions { get; }
        DbSet<Refund> Refunds { get; }
        DbSet<Seat> Seats { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

