using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace SddlReferral.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppDownloads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FpId = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    ReferralId = table.Column<string>(type: "text", nullable: false),
                    ReferralCode = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppDownloads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Referrals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReferralId = table.Column<string>(type: "text", nullable: true),
                    ReferralCode = table.Column<string>(type: "text", nullable: false),
                    ReferrerUserId = table.Column<string>(type: "text", nullable: true),
                    RefereeUserId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppDownload_FpId",
                table: "AppDownloads",
                column: "FpId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Referral_ReferralId",
                table: "Referrals",
                column: "ReferralId",
                unique: true)
                .Annotation("Npgsql:NullsDistinct", true);

            migrationBuilder.CreateIndex(
                name: "IX_Referral_ReferrerUserId_Status",
                table: "Referrals",
                columns: new[] { "ReferrerUserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppDownloads");

            migrationBuilder.DropTable(
                name: "Referrals");
        }
    }
}
