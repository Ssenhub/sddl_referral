namespace SddlReferral.Data
{
    using SddlReferral.Models;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class SddlReferralRepository : ISddlReferralRepository
    {
        private SddlReferralDbContext dbContext;

        public SddlReferralDbSet<Referral> Referrals { get; }

        public SddlReferralDbSet<AppDownload> AppDownloads { get; }

        public SddlReferralRepository(SddlReferralDbContext context)
        {
            this.dbContext = context;

            this.Referrals = new SddlReferralDbSet<Referral>(context.Referrals);
            this.AppDownloads = new SddlReferralDbSet<AppDownload>(context.AppDownloads);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await this.dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
