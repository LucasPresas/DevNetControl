using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevNetControl.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixMultiTenancyHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditTransactions_Users_FromUserId",
                table: "CreditTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_CreditTransactions_Users_ToUserId",
                table: "CreditTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_PlanAccesses_Users_UserId1",
                table: "PlanAccesses");

            migrationBuilder.DropForeignKey(
                name: "FK_Plans_Tenants_TenantId",
                table: "Plans");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogs_Tenants_TenantId",
                table: "SessionLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogs_Users_UserId",
                table: "SessionLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_VpsNodes_Tenants_TenantId",
                table: "VpsNodes");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_Subdomain",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_PlanAccesses_UserId1",
                table: "PlanAccesses");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "PlanAccesses");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditTransactions_Users_FromUserId",
                table: "CreditTransactions",
                column: "FromUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditTransactions_Users_ToUserId",
                table: "CreditTransactions",
                column: "ToUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Plans_Tenants_TenantId",
                table: "Plans",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogs_Tenants_TenantId",
                table: "SessionLogs",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogs_Users_UserId",
                table: "SessionLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VpsNodes_Tenants_TenantId",
                table: "VpsNodes",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditTransactions_Users_FromUserId",
                table: "CreditTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_CreditTransactions_Users_ToUserId",
                table: "CreditTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Plans_Tenants_TenantId",
                table: "Plans");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogs_Tenants_TenantId",
                table: "SessionLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogs_Users_UserId",
                table: "SessionLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_VpsNodes_Tenants_TenantId",
                table: "VpsNodes");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "PlanAccesses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Subdomain",
                table: "Tenants",
                column: "Subdomain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanAccesses_UserId1",
                table: "PlanAccesses",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditTransactions_Users_FromUserId",
                table: "CreditTransactions",
                column: "FromUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditTransactions_Users_ToUserId",
                table: "CreditTransactions",
                column: "ToUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlanAccesses_Users_UserId1",
                table: "PlanAccesses",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Plans_Tenants_TenantId",
                table: "Plans",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogs_Tenants_TenantId",
                table: "SessionLogs",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogs_Users_UserId",
                table: "SessionLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VpsNodes_Tenants_TenantId",
                table: "VpsNodes",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
