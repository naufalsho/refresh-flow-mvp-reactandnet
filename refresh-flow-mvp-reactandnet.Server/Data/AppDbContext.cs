using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using refresh_flow_mvp_reactandnet.Server.Entities;

namespace refresh_flow_mvp_reactandnet.Server.Data
{
    public class AppUser : IdentityUser { /* tambahkan properti jika perlu */ }
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);
            b.Entity<RefreshToken>()
                .HasIndex(x => x.TokenHash)
                .IsUnique();
            b.Entity<RefreshToken>()
                .HasIndex(x => new { x.UserId, x.FamilyId });
        }
    }
}