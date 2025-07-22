namespace SddlReferral.Controllers
{
    using Base62;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.OData.Formatter;
    using Microsoft.AspNetCore.OData.Query;
    using Microsoft.AspNetCore.OData.Routing.Controllers;
    using SddlReferral.Data;
    using SddlReferral.Models;
    using SddlReferral.Utils;
    using System.Linq;
    using System.Threading.Tasks;

    public class ReferralsController : ODataController
    {
        private readonly ISddlReferralRepository dbRepo;
        
        private readonly ILogger<ReferralsController> logger;

        public ReferralsController(ISddlReferralRepository repository, ILogger<ReferralsController> logger)
        {
            this.dbRepo = repository;
            this.logger = logger;
        }

        [Authorize]
        [HttpPost("NewReferral")]
        public async Task<IActionResult> NewReferral([FromBody] Referral referral)
        {
            if (!this.ModelState.IsValid)
            {
                string ms = Newtonsoft.Json.JsonConvert.SerializeObject(this.ModelState);
                
                this.logger.LogError($"Serialized model state: {ms}");

                return this.BadRequest(this.ModelState);
            }

            if (referral == null || string.IsNullOrWhiteSpace(referral.ReferralCode))
            {
                this.logger.LogError($"Referral or ReferralCode is empty");

                return this.BadRequest("Request body not supported");
            }

            referral.ReferrerUserId = Utility.GetUserId(this.User);

            if (string.IsNullOrWhiteSpace(referral.ReferrerUserId))
            {
                this.logger.LogError($"Empty ReferrerUserId");

                return this.StatusCode(500, "Empty user id");
            }

            referral.Status = ReferralStatus.Pending;
            referral.CreatedAt = DateTime.UtcNow;

            this.dbRepo.Referrals.Add(referral);

            try
            {
                await this.dbRepo.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError($"Adding new referral to DB failed. {ex}");

                return this.StatusCode(500, ex.Message);
            }

            // Prepend a non zero character to prevent OData from trimming leading '0' which can occur in Base62 encoded string.
            // TODO: Ideally an increment counter should read from a distributed cache like Redis
            // and the referral id (tiny url code) should be base62 encoding of that counter. 
            // For the purpose of this exercise, the incremental counter is generated from DB incremental key.
            referral.ReferralId = $"r{referral.Id.ToBase62()}";

            this.dbRepo.Referrals.Update(referral);

            try
            {
                await this.dbRepo.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError($"Updating referral id in DB failed. {ex}");

                return this.StatusCode(500, ex.Message);
            }

            return this.Created(referral);
        }

        [Authorize]
        [EnableQuery]
        public IActionResult Get(ODataQueryOptions<Referral> queryOptions)
        {
            string userId = Utility.GetUserId(this.User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                this.logger.LogError($"Empty user id");

                return this.StatusCode(500, "Empty user id");
            }

            IQueryable<Referral> userReferrals = this.dbRepo.Referrals.Where(r => r.ReferrerUserId == userId).AsQueryable<Referral>();

            if (queryOptions.Filter != null)
            {
                userReferrals = queryOptions.Filter.ApplyTo(userReferrals, new ODataQuerySettings()) as IQueryable<Referral>;
            }

            return Ok(userReferrals ?? Enumerable.Empty<Referral>().AsQueryable());
        }

        [Authorize]
        [HttpPut("CompleteReferral/{referralId}")]
        public async Task<IActionResult> CompleteReferral([FromODataUri] string referralId)
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

            Referral referral = await dbRepo.Referrals.FirstOrDefaultAsync(x => x.ReferralId == referralId).ConfigureAwait(false);

            if (referral == null)
            {
                return this.NotFound($"Referral Id ({referralId}) is not found");
            }

            referral.RefereeUserId = Utility.GetUserId(this.User);

            if (string.IsNullOrWhiteSpace(referral.RefereeUserId))
            {
                this.logger.LogError($"Empty RefereeUserId");

                return this.StatusCode(500, "Empty user id");
            }

            referral.Status = ReferralStatus.Completed;

            this.dbRepo.Referrals.Update(referral);

            try
            {
                await this.dbRepo.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError($"CompleteReferral failed to update DB. {ex}");

                return this.StatusCode(500, ex.Message);
            }

            return this.Ok(referral);
        }
    }
}