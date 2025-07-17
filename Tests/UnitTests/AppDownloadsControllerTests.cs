namespace SddlReferralTests.UnitTests
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.OData.Results;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;
    using SddlReferral.Controllers;
    using SddlReferral.Data;
    using SddlReferral.Models;
    using SddlReferral.Settings;
    using SddlReferral.Utils;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq.Expressions;
    using System.Net;
    using System.Security.Claims;
    using System.Threading.Tasks;

    [ExcludeFromCodeCoverage]
    [TestClass]
    public sealed class AppDownloadsControllerTests
    {
        #region States

        private static ILogger<ReferralsController> referralLogger = Substitute.For<ILogger<ReferralsController>>();
        private static ILogger<AppDownloadsController> appDownloadLogger = Substitute.For<ILogger<AppDownloadsController>>();
        private static ISddlReferralRepository mockContext;
        private static SddlReferralDbSet<AppDownload> mockAppDownloadDataset;
        private static SddlReferralDbSet<Referral> mockReferralDataset;
        private static IOptions<AppSettings> options = Substitute.For<IOptions<AppSettings>>();

        private static int dbContextSaveCount = 0;

        private static TestConfig testConfig;

        private static List<Referral> referrals;
        private static List<AppDownload> appDownloads;

        #endregion

        #region Setup

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            // mockReferralDataset
            mockReferralDataset = Substitute.For<SddlReferralDbSet<Referral>>(Substitute.For<DbSet<Referral>>());
            mockReferralDataset.FirstOrDefaultAsync(null).ReturnsForAnyArgs(
                ci =>
                {
                    Expression<Func<Referral, bool>> predicate = ci.ArgAt<Expression<Func<Referral, bool>>>(0);
                    return referrals.FirstOrDefault(predicate.Compile());
                });
            mockReferralDataset.Add(Arg.Do<Referral>(t => referrals.Add(t)));
            mockReferralDataset.Update(Arg.Do<Referral>(t =>
            {
                Referral reff = referrals.FirstOrDefault(r => r.Id == t.Id);
                reff.RefereeUserId = t.RefereeUserId;
                reff.ReferralCode = t.ReferralCode;
                reff.ReferralId = t.ReferralId;
                reff.ReferrerUserId = t.ReferrerUserId;
                reff.Status = t.Status;
            }));
            mockReferralDataset.Where(null).ReturnsForAnyArgs(
                ci =>
                {
                    Expression<Func<Referral, bool>> predicate = ci.ArgAt<Expression<Func<Referral, bool>>>(0);
                    return referrals.Where(predicate.Compile()).AsQueryable<Referral>();
                });

            // mockAppDownloadDataset
            mockAppDownloadDataset = Substitute.For<SddlReferralDbSet<AppDownload>>(Substitute.For<DbSet<AppDownload>>());
            mockAppDownloadDataset.FirstOrDefaultAsync(null).ReturnsForAnyArgs(
                ci =>
                {
                    Expression<Func<AppDownload, bool>> predicate = ci.ArgAt<Expression<Func<AppDownload, bool>>>(0);
                    return appDownloads.FirstOrDefault(predicate.Compile());
                });
            mockAppDownloadDataset.Add(Arg.Do<AppDownload>(t => appDownloads.Add(t)));
            mockAppDownloadDataset.Update(Arg.Do<AppDownload>(t =>
            {
                AppDownload d = appDownloads.FirstOrDefault(r => r.Id == t.Id);
                d.FpId = t.FpId;
                d.IpAddress = t.IpAddress;
                d.UserAgent = t.UserAgent;
                d.ReferralId = t.ReferralId;
            }));
            mockAppDownloadDataset.Where(null).ReturnsForAnyArgs(
                ci =>
                {
                    Expression<Func<AppDownload, bool>> predicate = ci.ArgAt<Expression<Func<AppDownload, bool>>>(0);
                    return appDownloads.Where(predicate.Compile()).AsQueryable<AppDownload>();
                });

            mockContext = Substitute.For<ISddlReferralRepository>();
            mockContext.Referrals.Returns(ci => mockReferralDataset);
            mockContext.AppDownloads.Returns(ci => mockAppDownloadDataset);
            mockContext.SaveChangesAsync().Returns(
                ci =>
                {
                    dbContextSaveCount++;

                    if (testConfig.TestDbFailure)
                    {
                        throw new Exception("Db save failed");
                    }

                    return 0;
                });

            options.Value.Returns(new AppSettings
            {  
                LinkExpirationPeriod = TimeSpan.FromMinutes(1),
                AndroidAppLink = "https://play.google.com/store/apps/details?id=com.cartoncaps.package",
                IosAppLink = "https://apps.apple.com/app/id123456789"
            });
        }

        [TestInitialize]
        public void TestInitialize()
        {
            referrals = new List<Referral>();
            appDownloads = new List<AppDownload>();
            dbContextSaveCount = 0;
            testConfig = new TestConfig();
        }

        #endregion

        #region Tests

        #region NewAppDownload

        [TestMethod]
        public async Task TestNewAppDownloadForAndroid()
        {
            var referralController = new ReferralsController(mockContext, referralLogger);
            referralController.ControllerContext = CreateReferralControllerContext("referrerUserId");

            // Create referral
            Referral referral = new Referral()
            {
                ReferralCode = "TESTCODE"
            };

            IActionResult result = await referralController.NewReferral(referral).ConfigureAwait(false);
            Referral referralResult = ((CreatedODataResult<Referral>)result).Value as Referral;

            AppDownloadsController appDownloadsController = new AppDownloadsController(mockContext, appDownloadLogger, options);
            appDownloadsController.ControllerContext = CreateAppDownloadControllerContext();
            appDownloadsController.Request.Headers["User-Agent"] = "Android";
            
            IActionResult result1 = await appDownloadsController.RedirectDownload(referralResult.ReferralId).ConfigureAwait(false);

            // Assert
            RedirectResult redirectResult = result1 as RedirectResult;
            Assert.AreEqual("https://play.google.com/store/apps/details?id=com.cartoncaps.package", redirectResult.Url);
            Assert.IsFalse(redirectResult.Permanent);

            Assert.IsTrue(appDownloadsController.HttpContext.Response.Headers.ContainsKey("Set-Cookie"));
            Assert.IsNotNull(appDownloadsController.HttpContext.Response.Headers["Set-Cookie"]);
        }

        [TestMethod]
        public async Task TestNewAppDownloadForIos()
        {
            var referralController = new ReferralsController(mockContext, referralLogger);
            referralController.ControllerContext = CreateReferralControllerContext("referrerUserId");

            // Create referral
            Referral referral = new Referral()
            {
                ReferralCode = "TESTCODE"
            };

            IActionResult result = await referralController.NewReferral(referral).ConfigureAwait(false);
            Referral referralResult = ((CreatedODataResult<Referral>)result).Value as Referral;

            AppDownloadsController appDownloadsController = new AppDownloadsController(mockContext, appDownloadLogger, options);
            appDownloadsController.ControllerContext = CreateAppDownloadControllerContext();
            appDownloadsController.Request.Headers["User-Agent"] = "Ios";

            IActionResult result1 = await appDownloadsController.RedirectDownload(referralResult.ReferralId).ConfigureAwait(false);

            // Assert
            RedirectResult redirectResult = result1 as RedirectResult;
            Assert.AreEqual("https://apps.apple.com/app/id123456789", redirectResult.Url);
            Assert.IsFalse(redirectResult.Permanent);

            Assert.IsTrue(appDownloadsController.HttpContext.Response.Headers.ContainsKey("Set-Cookie"));
            Assert.IsNotNull(appDownloadsController.HttpContext.Response.Headers["Set-Cookie"]);
        }

        [TestMethod]
        public async Task TestNewAppDownloadWithModelStateError()
        {
            AppDownloadsController appDownloadsController = new AppDownloadsController(mockContext, appDownloadLogger, options);
            
            // inject model state error
            appDownloadsController.ModelState.AddModelError("TestModelError", "Test model error");

            IActionResult result = await appDownloadsController.RedirectDownload("r0123").ConfigureAwait(false);

            // Assert
            Assert.IsTrue(result is BadRequestObjectResult);
            Assert.AreEqual(400, ((BadRequestObjectResult)result).StatusCode);
            Assert.AreEqual("Test model error", ((string[])((SerializableError)((BadRequestObjectResult)result).Value)["TestModelError"])[0]);
        }

        [TestMethod]
        public async Task TestNewAppDownloadWithWithEmptyParam()
        {
            AppDownloadsController appDownloadsController = new AppDownloadsController(mockContext, appDownloadLogger, options);

            IActionResult result = await appDownloadsController.RedirectDownload("").ConfigureAwait(false);

            // Assert
            Assert.IsTrue(result is BadRequestODataResult);
            Assert.AreEqual(400, ((BadRequestODataResult)result).StatusCode);
            Assert.AreEqual("referralId is empty", ((BadRequestODataResult)result).Error.Message);
        }

        [TestMethod]
        public async Task TestNewAppDownloadWithWithReferralIdNotFound()
        {
            AppDownloadsController appDownloadsController = new AppDownloadsController(mockContext, appDownloadLogger, options);

            IActionResult result = await appDownloadsController.RedirectDownload("123").ConfigureAwait(false);

            // Assert
            Assert.IsTrue(result is NotFoundODataResult);
            Assert.AreEqual(404, ((NotFoundODataResult)result).StatusCode);
            Assert.AreEqual($"Referral Id (123) is not found", ((NotFoundODataResult)result).Error.Message);
        }

        [TestMethod]
        public async Task TestNewAppDownloadWithExpiredLink()
        {
            var referralController = new ReferralsController(mockContext, referralLogger);
            referralController.ControllerContext = CreateReferralControllerContext("referrerUserId");

            // Create referral with old time stamp
            Referral referral = new Referral()
            {
                ReferralCode = "TESTCODE",
                CreatedAt = DateTime.UtcNow - TimeSpan.FromMinutes(2),
            };

            // Redirect link
            IActionResult result = await referralController.NewReferral(referral).ConfigureAwait(false);
            Referral referralResult = ((CreatedODataResult<Referral>)result).Value as Referral;

            AppDownloadsController appDownloadsController = new AppDownloadsController(mockContext, appDownloadLogger, options);
            appDownloadsController.ControllerContext = CreateAppDownloadControllerContext();
            appDownloadsController.Request.Headers["User-Agent"] = "Android";

            IActionResult result1 = await appDownloadsController.RedirectDownload(referralResult.ReferralId).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(result1 is BadRequestODataResult);
            Assert.AreEqual(400, ((BadRequestODataResult)result1).StatusCode);
            Assert.AreEqual("Link expired", ((BadRequestODataResult)result1).Error.Message);
        }

        [TestMethod]
        public async Task TestNewAppDownloadWithDbFailure()
        {
            var referralController = new ReferralsController(mockContext, referralLogger);
            referralController.ControllerContext = CreateReferralControllerContext("referrerUserId");

            // Create referral with old time stamp
            Referral referral = new Referral()
            {
                ReferralCode = "TESTCODE",
            };

            IActionResult result = await referralController.NewReferral(referral).ConfigureAwait(false);

            // Redirect link
            Referral referralResult = ((CreatedODataResult<Referral>)result).Value as Referral;
            
            AppDownloadsController appDownloadsController = new AppDownloadsController(mockContext, appDownloadLogger, options);
            appDownloadsController.ControllerContext = CreateAppDownloadControllerContext();
            appDownloadsController.Request.Headers["User-Agent"] = "Android";
            
            testConfig.TestDbFailure = true;

            IActionResult result1 = await appDownloadsController.RedirectDownload(referralResult.ReferralId).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(result1 is ObjectResult);
            Assert.AreEqual(500, ((ObjectResult)result1).StatusCode);
            Assert.AreEqual("Db save failed", ((ObjectResult)result1).Value);
        }

        [TestMethod]
        public async Task TestNewAppDownloadWithUnsupportedDevice()
        {
            var referralController = new ReferralsController(mockContext, referralLogger);
            referralController.ControllerContext = CreateReferralControllerContext("referrerUserId");

            // Create referral with old time stamp
            Referral referral = new Referral()
            {
                ReferralCode = "TESTCODE",
            };

            IActionResult result = await referralController.NewReferral(referral).ConfigureAwait(false);

            // Redirect link
            Referral referralResult = ((CreatedODataResult<Referral>)result).Value as Referral;

            AppDownloadsController appDownloadsController = new AppDownloadsController(mockContext, appDownloadLogger, options);
            appDownloadsController.ControllerContext = CreateAppDownloadControllerContext();
            appDownloadsController.Request.Headers["User-Agent"] = "Unknown";

            IActionResult result1 = await appDownloadsController.RedirectDownload(referralResult.ReferralId).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(result1 is ObjectResult);
            Assert.AreEqual(500, ((ObjectResult)result1).StatusCode);
            Assert.AreEqual("Unsupported device", ((ObjectResult)result1).Value);
        }

        #endregion

        #region ValidateReferral

        [TestMethod]
        public async Task TestValidateReferral()
        {
            var referralController = new ReferralsController(mockContext, referralLogger);
            referralController.ControllerContext = CreateReferralControllerContext("referrerUserId");

            // Create referral
            Referral referral = new Referral()
            {
                ReferralCode = "TESTCODE"
            };

            IActionResult result = await referralController.NewReferral(referral).ConfigureAwait(false);
            
            // Add download for that referral
            Referral referralResult = ((CreatedODataResult<Referral>)result).Value as Referral;

            AppDownloadsController appDownloadsController = new AppDownloadsController(mockContext, appDownloadLogger, options);
            appDownloadsController.ControllerContext = CreateAppDownloadControllerContext();
            appDownloadsController.Request.Headers["User-Agent"] = "Android";

            IActionResult result1 = await appDownloadsController.RedirectDownload(referralResult.ReferralId).ConfigureAwait(false);

            // Validate referral
            string fpId = ((string)appDownloadsController.HttpContext.Response.Headers["Set-Cookie"]).Split(":")[1];
            IActionResult result2 = await appDownloadsController.ValidateReferral(new Guid(fpId)).ConfigureAwait(false);

            Assert.IsTrue(result2 is OkObjectResult);
            AppDownload appDownloadResult = ((OkObjectResult)result2).Value as AppDownload;

            Assert.IsNotNull(appDownloadResult);
            Assert.AreEqual(fpId, appDownloadResult.FpId);
            Assert.AreEqual("Android", appDownloadResult.UserAgent);
            Assert.AreEqual("1.2.3.234", appDownloadResult.IpAddress);
            Assert.AreEqual(referralResult.ReferralId, appDownloadResult.ReferralId);
            Assert.AreEqual(referralResult.ReferralCode, appDownloadResult.ReferralCode);
        }

        [TestMethod]
        public async Task TestValidateReferralWithModelStateError()
        {
            AppDownloadsController appDownloadsController = new AppDownloadsController(mockContext, appDownloadLogger, options);

            // inject model state error
            appDownloadsController.ModelState.AddModelError("TestModelError", "Test model error");

            // Validate referral
            IActionResult result = await appDownloadsController.ValidateReferral(Guid.NewGuid()).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(result is BadRequestObjectResult);
            Assert.AreEqual(400, ((BadRequestObjectResult)result).StatusCode);
            Assert.AreEqual("Test model error", ((string[])((SerializableError)((BadRequestObjectResult)result).Value)["TestModelError"])[0]);
        }

        [TestMethod]
        public async Task TestValidateReferralWithFingerprintNotFound()
        {
            AppDownloadsController appDownloadsController = new AppDownloadsController(mockContext, appDownloadLogger, options);

            // Validate referral
            Guid fpId = Guid.NewGuid();
            IActionResult result = await appDownloadsController.ValidateReferral(fpId).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(result is NotFoundODataResult);
            Assert.AreEqual(404, ((NotFoundODataResult)result).StatusCode);
            Assert.AreEqual($"Device fingerprint ({fpId}) is not found", ((NotFoundODataResult)result).Error.Message);
        }

        [TestMethod]
        public async Task TestValidateReferralWithIpAddressMismatch()
        {
            var referralController = new ReferralsController(mockContext, referralLogger);
            referralController.ControllerContext = CreateReferralControllerContext("referrerUserId");

            // Create referral
            Referral referral = new Referral()
            {
                ReferralCode = "TESTCODE"
            };

            IActionResult result = await referralController.NewReferral(referral).ConfigureAwait(false);

            // Add download for that referral
            Referral referralResult = ((CreatedODataResult<Referral>)result).Value as Referral;

            AppDownloadsController appDownloadsController = new AppDownloadsController(mockContext, appDownloadLogger, options);
            appDownloadsController.ControllerContext = CreateAppDownloadControllerContext();
            appDownloadsController.Request.Headers["User-Agent"] = "Android";

            IActionResult result1 = await appDownloadsController.RedirectDownload(referralResult.ReferralId).ConfigureAwait(false);

            // Validate referral from a different IP address
            AppDownloadsController appDownloadsController1 = new AppDownloadsController(mockContext, appDownloadLogger, options);
            appDownloadsController1.ControllerContext = CreateAppDownloadControllerContext("0.0.0.234");
            appDownloadsController1.Request.Headers["User-Agent"] = "Android";

            string fpId = ((string)appDownloadsController.HttpContext.Response.Headers["Set-Cookie"]).Split(":")[1];
            IActionResult result2 = await appDownloadsController1.ValidateReferral(new Guid(fpId)).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(result2 is BadRequestODataResult);
            Assert.AreEqual(400, ((BadRequestODataResult)result2).StatusCode);
            Assert.AreEqual("IP address mismatch", ((BadRequestODataResult)result2).Error.Message);
        }

        [TestMethod]
        public async Task TestValidateReferralWithUserAgentMismatch()
        {
            var referralController = new ReferralsController(mockContext, referralLogger);
            referralController.ControllerContext = CreateReferralControllerContext("referrerUserId");

            // Create referral
            Referral referral = new Referral()
            {
                ReferralCode = "TESTCODE"
            };

            IActionResult result = await referralController.NewReferral(referral).ConfigureAwait(false);

            // Add download for that referral
            Referral referralResult = ((CreatedODataResult<Referral>)result).Value as Referral;

            AppDownloadsController appDownloadsController = new AppDownloadsController(mockContext, appDownloadLogger, options);
            appDownloadsController.ControllerContext = CreateAppDownloadControllerContext();
            appDownloadsController.Request.Headers["User-Agent"] = "Android";

            IActionResult result1 = await appDownloadsController.RedirectDownload(referralResult.ReferralId).ConfigureAwait(false);

            // Validate referral from a different IP address
            AppDownloadsController appDownloadsController1 = new AppDownloadsController(mockContext, appDownloadLogger, options);
            appDownloadsController1.ControllerContext = CreateAppDownloadControllerContext();
            appDownloadsController1.Request.Headers["User-Agent"] = "Ios";

            string fpId = ((string)appDownloadsController.HttpContext.Response.Headers["Set-Cookie"]).Split(":")[1];
            IActionResult result2 = await appDownloadsController1.ValidateReferral(new Guid(fpId)).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(result2 is BadRequestODataResult);
            Assert.AreEqual(400, ((BadRequestODataResult)result2).StatusCode);
            Assert.AreEqual("User agent mismatch", ((BadRequestODataResult)result2).Error.Message);
        }

        [TestMethod]
        public async Task TestValidateReferralWithCompletedReferral()
        {
            var referralController = new ReferralsController(mockContext, referralLogger);
            referralController.ControllerContext = CreateReferralControllerContext("referrerUserId");

            // Create referral
            Referral referral = new Referral()
            {
                ReferralCode = "TESTCODE"
            };

            IActionResult result = await referralController.NewReferral(referral).ConfigureAwait(false);

            // Add download for that referral
            Referral referralResult = ((CreatedODataResult<Referral>)result).Value as Referral;

            AppDownloadsController appDownloadsController = new AppDownloadsController(mockContext, appDownloadLogger, options);
            appDownloadsController.ControllerContext = CreateAppDownloadControllerContext();
            appDownloadsController.Request.Headers["User-Agent"] = "Android";

            IActionResult result1 = await appDownloadsController.RedirectDownload(referralResult.ReferralId).ConfigureAwait(false);

            // Validate referral
            AppDownloadsController appDownloadsController1 = new AppDownloadsController(mockContext, appDownloadLogger, options);
            appDownloadsController1.ControllerContext = CreateAppDownloadControllerContext();
            appDownloadsController1.Request.Headers["User-Agent"] = "Android";

            string fpId = ((string)appDownloadsController.HttpContext.Response.Headers["Set-Cookie"]).Split(":")[1];
            IActionResult result2 = await appDownloadsController1.ValidateReferral(new Guid(fpId)).ConfigureAwait(false);

            // Complete the referral
            AppDownload validatedDownload = ((OkObjectResult)result2).Value as AppDownload;
            referralController.ControllerContext = CreateReferralControllerContext("refereeUserId");
            IActionResult result3 = await referralController.CompleteReferral(validatedDownload.ReferralId).ConfigureAwait(false);
            
            // Assert
            Referral completedReferralResult = ((OkObjectResult)result3).Value as Referral;
            Assert.AreEqual("TESTCODE", completedReferralResult.ReferralCode);
            Assert.AreEqual("referrerUserId", completedReferralResult.ReferrerUserId);
            Assert.AreEqual("refereeUserId", completedReferralResult.RefereeUserId);
            Assert.AreEqual(ReferralStatus.Completed, completedReferralResult.Status);

            // Validate referral again for a completed referral
            IActionResult result4 = await appDownloadsController1.ValidateReferral(new Guid(fpId)).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(result4 is BadRequestODataResult);
            Assert.AreEqual(400, ((BadRequestODataResult)result4).StatusCode);
            Assert.AreEqual($"Referral Id ('{validatedDownload.ReferralId}') for device fingerprint ('{fpId}') is already completed", ((BadRequestODataResult)result4).Error.Message);
        }

        #endregion

        #endregion

        #region Privates

        private static ControllerContext CreateReferralControllerContext(string userdId)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userdId),
            };

            ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuthType");
            ClaimsPrincipal user = new ClaimsPrincipal(identity);

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user
                }
            };
        }

        private static ControllerContext CreateAppDownloadControllerContext(string ipAddress = null)
        {
            ConnectionInfo connection = Substitute.For<ConnectionInfo>();
            connection.RemoteIpAddress.Returns(IPAddress.Parse(ipAddress ?? "1.2.3.234"));

            HeaderDictionary headers = new HeaderDictionary();

            IResponseCookies cookie = Substitute.For<IResponseCookies>();
            cookie
                .When(c => c.Append(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CookieOptions>()))
                .Do(
                    ci =>
                    {
                        string k = ci.ArgAt<string>(0);
                        string v = ci.ArgAt<string>(1);
                        headers["Set-Cookie"] = $"{k}:{v}";
                    });

            HttpResponse httpResponse = Substitute.For<HttpResponse>();
            httpResponse.Headers.Returns(headers);
            httpResponse.Cookies.Returns(cookie);

            var context = Substitute.For<HttpContext>();
            context.Connection.Returns(connection);
            context.Response.Returns(httpResponse);

            return new ControllerContext
            {
                HttpContext = context,
            };
        }

        #endregion

        #region Util classes

        private class TestConfig
        {
            public bool TestDbFailure = false;
        }

        #endregion
    }
}
