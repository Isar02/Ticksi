using System.Security.Cryptography;
using System.Text;
using API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace API.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, IConfiguration configuration)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Read role names from configuration
        var roleNames = configuration.GetSection("Seeding:DefaultRoles").Get<string[]>() 
            ?? new[] { "Admin", "User", "Organizer" };

        // Seed Roles if they don't exist
        if (!await context.Roles.AnyAsync())
        {
            var roles = roleNames.Select(roleName => new Role 
            { 
                Name = roleName, 
                PublicId = Guid.NewGuid().ToString() 
            }).ToArray();

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
        }

        // Seed Admin User if it doesn't exist
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole == null)
            throw new Exception("Admin role not found. Please ensure roles are seeded first.");

        var adminEmail = configuration["Seeding:AdminEmail"] ?? "admin@ticksi.com";
        var adminExists = await context.AppUsers.AnyAsync(u => u.Email == adminEmail);

        if (!adminExists)
        {
            var defaultPassword = configuration["Seeding:DefaultPassword"] ?? "Admin123!";
            var adminPhone = configuration["Seeding:AdminPhone"] ?? "+38761123456";
            var defaultStatus = configuration["Seeding:DefaultStatus"] ?? "Active";

            var adminUser = new AppUser
            {
                FirstName = "Admin",
                LastName = "User",
                Email = adminEmail,
                PasswordHash = HashPassword(defaultPassword),
                Phone = adminPhone,
                RegistrationDate = DateTime.UtcNow,
                Status = defaultStatus,
                RoleId = adminRole.Id,
                PublicId = Guid.NewGuid().ToString()
            };

            await context.AppUsers.AddAsync(adminUser);
            await context.SaveChangesAsync();
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}

