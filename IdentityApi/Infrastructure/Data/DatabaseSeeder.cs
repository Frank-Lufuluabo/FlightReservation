using IdentityApi.Domain;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace IdentityApi.Infrastructure.Data
{
    public class DatabaseSeeder
    {
        public static async Task SeedAsync (AppDbContext context, UserManager<AppUser> userManager)
        {
            await context.Database.EnsureCreatedAsync();
            if (!context.Users.Any()) 
            {
                var admin = new AppUser
                {
                    Fullname = "Administrator",
                    UserName = "admin@admin.com",
                    PasswordHash = "Admin@123",
                    Email = "admin@admin.com"
                };
                await userManager.CreateAsync(admin, admin.PasswordHash);
                List<Claim> claims = [
                    new(ClaimTypes.Role, "")
                    ];
            }

        }
    }
}
