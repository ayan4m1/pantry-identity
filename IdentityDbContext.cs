using Microsoft.EntityFrameworkCore;
using Identity.Entities;

namespace Identity
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
                e.HasKey(h => h.Id);
                e.Property(h => h.Name).HasMaxLength(100).IsRequired();
                e.Property(h => h.CreatedByUserId).HasMaxLength(64).IsRequired();

                e.HasMany(h => h.Members)
                 .WithOne(m => m.Household)
                 .HasForeignKey(m => m.HouseholdId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(h => h.Invites)
                 .WithOne(i => i.Household)
                 .HasForeignKey(i => i.HouseholdId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<HouseholdMember>(e =>
            {
                e.HasKey(m => new { m.HouseholdId, m.UserId });
                e.HasIndex(m => m.UserId);                       // "which households am I in?"

                e.Property(m => m.UserId).HasMaxLength(64);
                e.Property(m => m.Role).HasConversion<string>().HasMaxLength(16);
            });

            b.Entity<Invite>(e =>
            {
                e.HasKey(i => i.Id);
                e.Property(i => i.Email).HasMaxLength(254).IsRequired();
                e.Property(i => i.Token).HasMaxLength(64).IsRequired();
                e.Property(i => i.InvitedByUserId).HasMaxLength(64).IsRequired();
                e.Property(i => i.AcceptedByUserId).HasMaxLength(64);
                e.Property(i => i.Status).HasConversion<string>().HasMaxLength(16);

                e.HasIndex(i => i.Token).IsUnique();
                e.HasIndex(i => new { i.HouseholdId, i.Email })
                 .IsUnique()
                 .HasFilter("status = 'Pending'");               // one pending invite per email per household
            });
        }
    }
}
