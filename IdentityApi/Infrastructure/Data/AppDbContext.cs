using IdentityApi.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityApi.Infrastructure.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options): IdentityDbContext<AppUser>(options)
    {
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    }
}
