namespace SddlReferral.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.OData.Formatter;
    using Microsoft.AspNetCore.OData.Query;
    using Microsoft.AspNetCore.OData.Routing.Controllers;
    using Microsoft.Extensions.Options;
    using SddlReferral.Data;
    using SddlReferral.Models;
    using SddlReferral.Settings;
    using SddlReferral.Utils;
    using System.Threading.Tasks;

    public class AppDownloadsController : ODataController
    {
        private readonly ISddlReferralRepository dbContext;
        
        private readonly ILogger<AppDownloadsController> logger;

        private readonly AppSettings settings;

        public AppDownloadsController(ISddlReferralRepository context, ILogger<AppDownloadsController> logger, IOptions<AppSettings> options)
        {
            this.dbContext = context;
            this.logger = logger;
            this.settings = options.Value;
        }

        /// <summary>
        /// This is called when sddl is tapped. 
        /// Client generate this URL after referral is created by calling /NewReferral endpoint
        /// referralId is retrieved from /NewReferral response
        /// Example:
        /// Request:
        ///     GET /download/r3TY
        /// Response:
        /// RawContent: HTTP/1.1 302 Found
        ///             Content-Length: 0
        ///             Date: Wed, 16 Jul 2025 00:58:49 GMT
        ///             Location: https://play.google.com/store/apps/details?id=com.cartoncaps.package
        ///             Set-Cookie: fpId=4bdb3a62-70e1-4a07-a56e-16d...
        ///             Headers: {[Location, https://play.google.com/store/apps/details?id=com.cartoncaps.package], [Set-Cookie, fpId=4bdb3a62-70e1-4a07-a56e-16db021e3848; path=/; httponly]}
        /// </summary>
        /// <param name="referralId">Referral id</param>
        /// <returns>Redirection to app url</returns>
        /// <example></example>
        [HttpGet("Download/{referralId}")]
        public async Task<IActionResult> RedirectDownload([FromODataUri] string referralId)
        {
            return await Utility.ExecuteAndThrow(
                async () =>
                {
                    if (!this.ModelState.IsValid)
                    {
                        return this.BadRequest(this.ModelState);
                    }

                    if (string.IsNullOrWhiteSpace(referralId))
                    {
                        this.logger.LogError("referralId is empty");

                        return this.BadRequest("referralId is empty");
                    }

                    Referral referral = await dbContext.Referrals.FirstOrDefaultAsync(r => r.ReferralId == referralId).ConfigureAwait(false);

                    // 1. Check if referral id exists
                    if (referral == null)
                    {
                        return this.NotFound($"Referral Id ({referralId}) is not found");
                    }

                    // 2. Check link expiration
                    if (DateTime.UtcNow - referral.CreatedAt > this.settings.LinkExpirationPeriod)
                    {
                        return this.BadRequest("Link expired");
                    }

                    // TODO 1: this.HttpContext.Connection.RemoteIpAddress gets populated with client IP address if this app is hosted directly
                    // and not behind a proxy or load balancer. It will need to handle X-Forwarded-For header for other network configurations.
                    // For the purpose of this exercise as a POC, this is out of scope. 
                    /// TODO 2: The device fingerprint + timestamp should be signed by a secret which is stored in a safe vault (could be Azure Key Vault).
                    /// Client should sent the fingerprint along with the signature to /validatereferral endpoint where the signature should be verified 
                    /// against the fingerprint and timestamp. 
                    AppDownload appDownload = new AppDownload
                    {
                        FpId = Guid.NewGuid().ToString(),
                        IpAddress = this.HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = this.Request.Headers["User-Agent"].ToString(),
                        ReferralId = referralId,
                        ReferralCode = referral.ReferralCode
                    };

                    this.dbContext.AppDownloads.Add(appDownload);

                    try
                    {
                        await this.dbContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError($"RedirectDownload failed to update DB. {ex}");

                        return this.StatusCode(500, ex.Message);
                    }

                    // Set cookie for correlation
                    this.Response.Cookies.Append("fpId", appDownload.FpId, new CookieOptions { HttpOnly = true });

                    if (appDownload.UserAgent.ToLower().Contains("iphone") || appDownload.UserAgent.ToLower().Contains("ipad") || appDownload.UserAgent.ToLower().Contains("ios"))
                    {
                        return this.Redirect(this.settings.IosAppLink);
                    }
                    else if (appDownload.UserAgent.ToLower().Contains("android"))
                    {
                        return this.Redirect(this.settings.AndroidAppLink);
                    }
                    else
                    {
                        this.logger.LogError($"Unsupported device. User agent: {appDownload.UserAgent}");

                        return this.StatusCode(500, "Unsupported device");
                    }
                },
                this.logger).ConfigureAwait(false);
        }

        [EnableQuery]
        [HttpGet("ValidateReferral/{fpId}")]
        public async Task<IActionResult> ValidateReferral([FromODataUri] Guid fpId)
        {
            return await Utility.ExecuteAndThrow(
                async () =>
                {
                    // Validations
                    // 1. Model state error
                    if (!this.ModelState.IsValid)
                    {
                        return this.BadRequest(this.ModelState);
                    }

                    AppDownload appDownload = await dbContext.AppDownloads.FirstOrDefaultAsync(x => x.FpId == fpId.ToString());

                    // 2. Check device fingerprint exists
                    if (appDownload == null)
                    {
                        return this.NotFound($"Device fingerprint ({fpId}) is not found");
                    }

                    // 3. Check IP address match
                    string ipAddress = this.HttpContext.Connection.RemoteIpAddress?.ToString();

                    if (ipAddress != appDownload.IpAddress)
                    {
                        return this.BadRequest($"IP address mismatch");
                    }

                    // 4. Check user agent match
                    string userAgent = this.Request.Headers["User-Agent"].ToString();

                    if (userAgent != appDownload.UserAgent)
                    {
                        return this.BadRequest($"User agent mismatch");
                    }

                    // 5. Check if referral id exists
                    Referral referral = await dbContext.Referrals.FirstOrDefaultAsync(r => r.ReferralId == appDownload.ReferralId).ConfigureAwait(false);

                    if (referral == null)
                    {
                        this.logger.LogError($"Inconsistent data: Referral Id ('{appDownload.ReferralId}') exists is AppDownload but not Referral");

                        return this.NotFound($"Referral Id ('{appDownload.ReferralId}') is not found");
                    }

                    // 6. Check if referral is in pending state
                    if (referral.Status != ReferralStatus.Pending)
                    {
                        return this.BadRequest($"Referral Id ('{appDownload.ReferralId}') for device fingerprint ('{fpId}') is already completed");
                    }

                    return this.Ok(appDownload);
                },
                this.logger).ConfigureAwait(false);
        }
    }
}