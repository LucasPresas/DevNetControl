using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevNetControl.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditSystemAndTrialSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProvisionedOnVps",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTrial",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialExpiry",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditsPerDevice",
                table: "Tenants",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TrialMaxHours",
                table: "Tenants",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TrialMaxPerReseller",
                table: "Tenants",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "CreditTransactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsProvisionedOnVps",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsTrial",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TrialExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreditsPerDevice",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TrialMaxHours",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TrialMaxPerReseller",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "CreditTransactions");
        }
    }
}
