namespace SddlReferral.Models
{
    using System.ComponentModel.DataAnnotations;

    public class AppDownload
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FpId { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        [Required]
        public string ReferralId { get; set; }

        public string? ReferralCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
