namespace SddlReferralTests.UnitTests
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.OData.Query;
    using Microsoft.AspNetCore.OData.Results;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.OData.Edm;
    using Microsoft.OData.ModelBuilder;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;
    using SddlReferral.Controllers;
    using SddlReferral.Data;
    using SddlReferral.Models;
    using SddlReferral.Utils;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq.Expressions;
    using System.Security.Claims;
    using System.Threading.Tasks;

    [ExcludeFromCodeCoverage]
    [TestClass]
    public sealed class ReferralsControllerTests
    {
        #region States

        private static ILogger<ReferralsController> logger = Substitute.For<ILogger<ReferralsController>>();
        private static ISddlReferralRepository mockContext;
        private static SddlReferralDbSet<Referral> mockDataset;

        private static int dbContextSaveCount = 0;

        private static TestConfig testConfig;

        private static List<Referral> referrals;

        #endregion

        #region Setup

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            //DbContextOptions<SddlReferralDbContext> dbContextOptions = new DbContextOptions<SddlReferralDbContext>();
            mockDataset = Substitute.For<SddlReferralDbSet<Referral>>(Substitute.For<DbSet<Referral>>());
            mockDataset.FirstOrDefaultAsync(null).ReturnsForAnyArgs(
                ci =>
                {
                    Expression<Func<Referral, bool>> predicate = ci.ArgAt<Expression<Func<Referral, bool>>>(0);
                    return referrals.FirstOrDefault(predicate.Compile());
                });
            mockDataset.Add(Arg.Do<Referral>(t => referrals.Add(t)));
            mockDataset.Update(Arg.Do<Referral>(t =>
                { 
                    Referral reff = referrals.FirstOrDefault(r => r.Id == t.Id);
                    reff.RefereeUserId = t.RefereeUserId;
                    reff.ReferralCode = t.ReferralCode;
                    reff.ReferralId = t.ReferralId;
                    reff.ReferrerUserId = t.ReferrerUserId;
                    reff.Status = t.Status;
                }));
            mockDataset.Where(null).ReturnsForAnyArgs(
                ci =>
                {
                    Expression<Func<Referral, bool>> predicate = ci.ArgAt<Expression<Func<Referral, bool>>>(0);
                    return referrals.Where(predicate.Compile()).AsQueryable<Referral>();
                });

            mockContext = Substitute.For<ISddlReferralRepository>();
            mockContext.Referrals.Returns(ci => mockDataset);
            mockContext.SaveChangesAsync().Returns(
                ci =>
                {
                    dbContextSaveCount++;

                    if (testConfig.TestNewReferralAddFailure && dbContextSaveCount == 1)
                    {
                        throw new Exception("New referral add failed");
                    }
                    else if (testConfig.TestUpdateReferralIdFailure && dbContextSaveCount == 2)
                    {
                        throw new Exception("Referral Id update failed");
                    }
                    else if (testConfig.TestDbFailure)
                    {
                        throw new Exception("Db save failed");
                    }

                    return 0;
                });
        }

        [TestInitialize]
        public void TestInitialize()
        {
            referrals = new List<Referral>();
            dbContextSaveCount = 0;
            testConfig = new TestConfig();
        }

        #endregion

        #region Tests

        #region NewReferral

        [TestMethod]
        public async Task TestNewReferral()
        {
            var controller = new ReferralsController(mockContext, logger);
            controller.ControllerContext = CreateControllerContext("referrerUserId");

            // Act
            Referral referral = new Referral()
            {
                ReferralCode = "TESTCODE"
            };

            IActionResult result = await controller.NewReferral(referral).ConfigureAwait(false);

            // Assert
            Referral referralResult = ((CreatedODataResult<Referral>)result).Value as Referral;
            Assert.AreEqual("TESTCODE", referralResult.ReferralCode);
            Assert.AreEqual("referrerUserId", referralResult.ReferrerUserId);
            Assert.IsNull(referralResult.RefereeUserId);
            Assert.AreEqual(ReferralStatus.Pending, referralResult.Status);
        }

        [TestMethod]
        public async Task TestInvalidRequestBodyInReferral()
        {
            var controller = new ReferralsController(mockContext, logger);
            controller.ControllerContext = CreateControllerContext("referrerUserId");

            // Act
            Referral referral = new Referral();
            IActionResult result = await controller.NewReferral(referral).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(result is BadRequestODataResult);
            Assert.AreEqual(400, ((BadRequestODataResult)result).StatusCode);
            Assert.AreEqual("Request body not supported", ((BadRequestODataResult)result).Error.Message);
        }

        [TestMethod]
        public async Task TestDbSaveFailureForNewReferral()
        {
            testConfig.TestNewReferralAddFailure = true;
            var controller = new ReferralsController(mockContext, logger);
            controller.ControllerContext = CreateControllerContext("referrerUserId");

            // Act
            Referral referral = new Referral()
            {
                ReferralCode = "TESTCODE"
            };

            IActionResult result = await controller.NewReferral(referral).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(result is ObjectResult);
            Assert.AreEqual(500, ((ObjectResult)result).StatusCode);
            Assert.AreEqual("New referral add failed", ((ObjectResult)result).Value);
        }

        [TestMethod]
        public async Task TestReferralIdUpdateFailureForNewReferral()
        {
            testConfig.TestUpdateReferralIdFailure = true;

            var controller = new ReferralsController(mockContext, logger);
            controller.ControllerContext = CreateControllerContext("referrerUserId");

            // Act
            Referral referral = new Referral()
            {
                ReferralCode = "TESTCODE"
            };

            IActionResult result = await controller.NewReferral(referral).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(result is ObjectResult);
            Assert.AreEqual(500, ((ObjectResult)result).StatusCode);
            Assert.AreEqual("Referral Id update failed", ((ObjectResult)result).Value);
        }

        #endregion

        #region GetReferrals

        [TestMethod]
        public async Task TestGet()
        {
            referrals = new List<Referral>()
            {
                new Referral
                {
                     Id = 1,
                     ReferralCode = "RCODE1",
                     ReferrerUserId = "referrerUserId",
                     RefereeUserId = "refereeUserId",
                     ReferralId = "rn",
                     Status = ReferralStatus.Completed,
                     CreatedAt = DateTime.UtcNow,
                },
                new Referral
                {
                     Id = 2,
                     ReferralCode = "RCODE2",
                     ReferrerUserId = "referrerUserId",
                     RefereeUserId = "refereeUserId",
                     ReferralId = "ro",
                     Status = ReferralStatus.Pending,
                     CreatedAt = DateTime.UtcNow,
                },
                new Referral
                {
                     Id = 3,
                     ReferralCode = "RCODE3",
                     ReferrerUserId = "referrerUserId1",
                     RefereeUserId = "refereeUserId",
                     ReferralId = "rp",
                     Status = ReferralStatus.Completed,
                     CreatedAt = DateTime.UtcNow,
                }
            };

            //Save referral 1
            ReferralsController controller = new ReferralsController(mockContext, logger);
            controller.ControllerContext = CreateControllerContext("referrerUserId");

            // Get for user referrerUserId
            ODataQueryOptions<Referral> queryOptions = CreateODataQueryOptions("?$filter=status eq 1");

            IActionResult result = await controller.Get(queryOptions).ConfigureAwait(false);

            IQueryable<Referral> referralResult = ((OkObjectResult)result).Value as IQueryable<Referral>;

            // Assert
            Assert.IsNotNull(referralResult);
            Assert.AreEqual(1, referralResult.Count());
            Assert.AreEqual(referrals[0].Id, referralResult.ElementAt(0).Id);
        }

        #endregion

        #region CompleteReferrals
        
        [TestMethod]
        public async Task TestCompleteReferral()
        {
            var controller = new ReferralsController(mockContext, logger);
            controller.ControllerContext = CreateControllerContext("referrerUserId");

            // Act
            Referral referral = new Referral()
            {
                ReferralCode = "TESTCODE"
            };

            IActionResult result = await controller.NewReferral(referral).ConfigureAwait(false);

            // Assert
            Referral referralResult = ((CreatedODataResult<Referral>)result).Value as Referral;
            Assert.AreEqual("TESTCODE", referralResult.ReferralCode);
            Assert.AreEqual("referrerUserId", referralResult.ReferrerUserId);
            Assert.IsNull(referralResult.RefereeUserId);
            Assert.AreEqual(ReferralStatus.Pending, referralResult.Status);

            IActionResult result1 = await controller.CompleteReferral(referralResult.ReferralId).ConfigureAwait(false);

            Referral completedReferralResult = ((OkObjectResult)result1).Value as Referral;
            Assert.AreEqual("TESTCODE", completedReferralResult.ReferralCode);
            Assert.AreEqual("referrerUserId", completedReferralResult.ReferrerUserId);
            Assert.AreEqual("referrerUserId", completedReferralResult.RefereeUserId);
            Assert.AreEqual(ReferralStatus.Completed, completedReferralResult.Status);
        }

        [TestMethod]
        public async Task TestCompleteReferralModelStateError()
        {
            var controller = new ReferralsController(mockContext, logger);
            controller.ControllerContext = CreateControllerContext("referrerUserId");
            
            // inject model state error
            controller.ModelState.AddModelError("TestModelError", "Test model error");
            IActionResult result = await controller.CompleteReferral("r012").ConfigureAwait(false);

            Assert.IsTrue(result is BadRequestObjectResult);
            Assert.AreEqual(400, ((BadRequestObjectResult)result).StatusCode);
            Assert.AreEqual("Test model error", ((string[])((SerializableError)((BadRequestObjectResult)result).Value)["TestModelError"])[0]);
        }

        [TestMethod]
        public async Task TestCompleteReferralWithEmptyParam()
        {
            var controller = new ReferralsController(mockContext, logger);
            controller.ControllerContext = CreateControllerContext("referrerUserId");

            // inject model state error
            IActionResult result = await controller.CompleteReferral(string.Empty).ConfigureAwait(false);

            Assert.IsTrue(result is BadRequestODataResult);
            Assert.AreEqual(400, ((BadRequestODataResult)result).StatusCode);
            Assert.AreEqual("referralId is empty", ((BadRequestODataResult)result).Error.Message);
        }

        [TestMethod]
        public async Task TestCompleteReferralWithNotFoundReferralId()
        {
            var controller = new ReferralsController(mockContext, logger);
            controller.ControllerContext = CreateControllerContext("referrerUserId");

            // inject model state error
            IActionResult result = await controller.CompleteReferral("r012").ConfigureAwait(false);

            // Assert
            Assert.IsTrue(result is NotFoundODataResult);
            Assert.AreEqual(404, ((NotFoundODataResult)result).StatusCode);
            Assert.AreEqual($"Referral Id (r012) is not found", ((NotFoundODataResult)result).Error.Message);
        }

        [TestMethod]
        public async Task TestCompleteReferralWithDbError()
        {
            var controller = new ReferralsController(mockContext, logger);
            controller.ControllerContext = CreateControllerContext("referrerUserId");

            // Act
            Referral referral = new Referral()
            {
                ReferralCode = "TESTCODE"
            };

            IActionResult result = await controller.NewReferral(referral).ConfigureAwait(false);

            // Assert
            Referral referralResult = ((CreatedODataResult<Referral>)result).Value as Referral;
            Assert.AreEqual("TESTCODE", referralResult.ReferralCode);
            Assert.AreEqual("referrerUserId", referralResult.ReferrerUserId);
            Assert.IsNull(referralResult.RefereeUserId);
            Assert.AreEqual(ReferralStatus.Pending, referralResult.Status);

            testConfig.TestDbFailure = true;
            IActionResult result1 = await controller.CompleteReferral(referralResult.ReferralId).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(result1 is ObjectResult);
            Assert.AreEqual(500, ((ObjectResult)result1).StatusCode);
            Assert.AreEqual("Db save failed", ((ObjectResult)result1).Value);
        }

        #endregion

        #endregion

        #region Privates

        private static ODataQueryOptions<Referral> CreateODataQueryOptions(string queryString)
        {
            // Create the EDM model
            var modelBuilder = new ODataConventionModelBuilder();
            modelBuilder.EntitySet<Referral>("Referrals");
            IEdmModel edmModel = modelBuilder.GetEdmModel();

            // Create OData query context
            var entityType = edmModel.SchemaElements
                .OfType<IEdmEntityType>()
                .First(e => e.Name == nameof(Referral));
            var entitySet = edmModel.EntityContainer.FindEntitySet("Referrals");

            var queryContext = new ODataQueryContext(edmModel, typeof(Referral), new Microsoft.OData.UriParser.ODataPath());

            // Create a fake HttpRequest with the query string
            var request = new DefaultHttpContext().Request;
            request.Method = "GET";
            request.QueryString = new QueryString(queryString);
            request.HttpContext.RequestServices = new ServiceCollection().BuildServiceProvider();
                //.AddOData()
                //.BuildServiceProvider();

            return new ODataQueryOptions<Referral>(queryContext, request);
        }

        private static ControllerContext CreateControllerContext(string userdId)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userdId),
                //new Claim(ClaimTypes.Name, "testuser@example.com"),
                //new Claim("custom-claim", "value123")
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

        #endregion

        #region Util classes
        
        private class TestConfig
        {
            public bool TestNewReferralAddFailure = false;
            public bool TestUpdateReferralIdFailure = false;
            public bool TestDbFailure = false;
        }

        #endregion
    }
}
