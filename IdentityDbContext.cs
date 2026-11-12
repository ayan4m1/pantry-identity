using Microsoft.EntityFrameworkCore;
using pantry_identity.Entities;

namespace pantry_identity
{
    public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
    {
        public DbSet<Household> Households => Set<Household>();
        public DbSet<HouseholdMember> HouseholdMembers => Set<HouseholdMember>();
        public DbSet<Invite> Invites => Set<Invite>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Household>(e =>
            {
                e.Property(h => h.Name).HasMaxLength(100);
            });

            b.Entity<HouseholdMember>(e =>
            {
                e.HasKey(m => new { m.HouseholdId, m.UserId });
                e.HasIndex(m => m.UserId); // "which households am I in?"
                e.Property(m => m.UserId).HasMaxLength(64);
                e.Property(m => m.Role).HasConversion<string>();
            });

            b.Entity<Invite>(e =>
            {
                e.HasIndex(i => i.Token).IsUnique();
                e.HasIndex(i => new { i.HouseholdId, i.Email })
                .HasFilter("status = 'Pending'") // one pending invite per email per household
                .IsUnique();
                e.Property(i => i.Email).HasMaxLength(254);
                e.Property(i => i.Status).HasConversion<string>();
            });
        }
    }
}
