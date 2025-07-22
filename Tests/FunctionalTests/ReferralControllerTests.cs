namespace SddlReferralTests.FunctionalTests
{
    using Microsoft.Playwright;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using SddlReferral.Models;
    using SddlReferral.Utils;
    using System.Security.Cryptography.Xml;
    using static System.Net.Mime.MediaTypeNames;

    [TestClass]
    public class ReferralControllerTests
    {
        private const string token_referrer = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiJ0ZXN0dXNlcklkIiwic3ViIjoiMTIzIiwibmFtZSI6InRlc3R1c2VyIiwiZW1haWwiOiJ0ZXN0QGV4YW1wbGUuY29tIiwicm9sZSI6IkFkbWluIn0.Kof-ftiTIyK44VZdwKCh5IpVaIfhP3M79huv5OTSdYU";
        private const string token_referee = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiJ0ZXN0dXNlcklkIiwic3ViIjoicmVmZXJlZWlkIiwibmFtZSI6InRlc3R1c2VyIiwiZW1haWwiOiJ0ZXN0QGV4YW1wbGUuY29tIiwicm9sZSI6IkFkbWluIn0.npL6LgnyEDzjaAbJw0pRn9tw2vfbv3mDRDlZbvLLrAM";
        private const string token_unauth = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWUsImlhdCI6MTUxNjIzOTAyMn0.3a11HOLS81EpF1X16MkbhU_MxM1hgZ6YIGTjpJD6o80";

        [TestMethod]
        public async Task GetReferrals_ReturnsOk()
        {
            using var playwright = await Playwright.CreateAsync();
            var request = await playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
            {
                BaseURL = "http://localhost:5259",
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    { "Accept", "application/json" },
                    { "Content-Type", "application/json" },
                    { "Authorization", $"Bearer {token_referrer}" }
                },
            });

            Referral body = new Referral
            {
                ReferralCode = "REFCODE"
            };

            IAPIResponse response = await request.PostAsync(
                "/NewReferral",
                new APIRequestContextOptions { DataObject = body });

            Assert.IsTrue(response.Ok);

            var responseBody = (await response.JsonAsync()).ToString();
            var responseReferral = Newtonsoft.Json.JsonConvert.DeserializeObject<Referral>(responseBody);
            
            Assert.IsNotNull(responseBody);
            Assert.AreEqual(body.ReferralCode, responseReferral?.ReferralCode);
            Assert.AreEqual(ReferralStatus.Pending, responseReferral?.Status);
        }
    }
}
