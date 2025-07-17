namespace SddlReferral.Data
{
    using Microsoft.EntityFrameworkCore;
    using SddlReferral.Models;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class SddlReferralDbContext : DbContext
    {
        public SddlReferralDbContext(DbContextOptions<SddlReferralDbContext> options)
            : base(options) { }

        public virtual DbSet<Referral> Referrals { get; set; }
        
        public virtual DbSet<AppDownload> AppDownloads { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Referral>(entity =>
            {
                // Primary key
                entity.HasKey(p => p.Id);

                entity.Property(p => p.ReferralId).IsRequired(false);
                entity.Property(p => p.RefereeUserId).IsRequired(false);
                entity.Property(p => p.ReferrerUserId).IsRequired(false);

                // Secondary index on ReferralId
                entity.HasIndex(p => new { p.ReferralId })
                      .HasDatabaseName("IX_Referral_ReferralId")
                      .IsUnique(true)
                      .AreNullsDistinct(true);

                // Secondary index on ReferrerUserId and Status
                entity.HasIndex(p => new { p.ReferrerUserId, p.Status })
                      .HasDatabaseName("IX_Referral_ReferrerUserId_Status")
                      .IsUnique(false);
            });

            modelBuilder.Entity<AppDownload>(entity =>
            {
                // Primary key
                entity.HasKey(p => p.Id);

                // Secondary index on FpId
                entity.HasIndex(p => new { p.FpId })
                      .HasDatabaseName("IX_AppDownload_FpId")
                      .IsUnique(true);
            });
        }
    }
}