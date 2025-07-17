using SddlReferral.Models;

namespace SddlReferral.Data
{
    public interface ISddlReferralRepository
    {
        SddlReferralDbSet<Referral> Referrals { get; }

        SddlReferralDbSet<AppDownload> AppDownloads { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
