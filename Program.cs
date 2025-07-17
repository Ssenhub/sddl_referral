namespace SddlReferral
{
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.OData;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.OData.ModelBuilder;
    using SddlReferral.Data;
    using SddlReferral.Models;
    using SddlReferral.Settings;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    [ExcludeFromCodeCoverage]
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var modelBuilder = new ODataConventionModelBuilder();
            
            // Entities
            modelBuilder.EntitySet<Referral>("Referrals");
            modelBuilder.EntitySet<AppDownload>("AppDownloads");

            ActionConfiguration newReferralAction = modelBuilder.Action("NewReferral");
            newReferralAction.Parameter<Referral>("referral");
            newReferralAction.ReturnsFromEntitySet<Referral>("Referrals");

            // DB
            builder.Services.AddDbContext<SddlReferralDbContext>(opt =>
                opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped(typeof(ISddlReferralRepository), typeof(SddlReferralRepository));

            // AuthN and AuthZ
            // Auth is out of scope for this exercise. This is added here to make sure user scoped APIs get user id from token.
            // Make sure the secret key matches with the token signing key. Other auth validations are turned off.
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = false,
                        ValidateIssuerSigningKey = false,
                        ValidIssuer = "your_issuer",
                        ValidAudience = "your_audience",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetValue<string>("JwtSecretKey")))
                    };
                });

            builder.Services.AddAuthorization();

            // Controllers
            builder.Services.AddControllers().AddOData(
                options =>
                {
                    options.Select().Filter().OrderBy().Expand().Count().SetMaxTop(null).AddRouteComponents(modelBuilder.GetEdmModel()).EnableQueryFeatures();
                });

            // Settings and configuration
            builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            // Logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();

            // App
            var app = builder.Build();

            // DB migration
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SddlReferralDbContext>();
                db.Database.Migrate();
            }

            // App middlewares
            app.UseForwardedHeaders();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapControllers());

            app.Run();
        }
    }
}
