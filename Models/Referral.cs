using SddlReferral.Utils;
using System.ComponentModel.DataAnnotations;

namespace SddlReferral.Models
{
    public class Referral
    {
        [Key]
        public int Id { get; set; }

        public string ReferralId { get; set; }

        [Required]
        public string ReferralCode { get; set; }

        public string ReferrerUserId { get; set; }

        public string RefereeUserId { get; set; }

        public ReferralStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
