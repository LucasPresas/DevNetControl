using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevNetControl.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixUserDeletionForeignKeyConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditTransactions_Users_SourceUserId",
                table: "CreditTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_CreditTransactions_Users_TargetUserId",
                table: "CreditTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogs_Users_UserId",
                table: "SessionLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Plans_PlanId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ParentId",
                table: "Users");

            migrationBuilder.AddColumn<Guid>(
                name: "PlanId1",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId1",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceUserId",
                table: "CreditTransactions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PlanId1",
                table: "Users",
                column: "PlanId1");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId1",
                table: "Users",
                column: "TenantId1");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditTransactions_Users_SourceUserId",
                table: "CreditTransactions",
                column: "SourceUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditTransactions_Users_TargetUserId",
                table: "CreditTransactions",
                column: "TargetUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogs_Users_UserId",
                table: "SessionLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Plans_PlanId",
                table: "Users",
                column: "PlanId",
                principalTable: "Plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Plans_PlanId1",
                table: "Users",
                column: "PlanId1",
                principalTable: "Plans",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId1",
                table: "Users",
                column: "TenantId1",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ParentId",
                table: "Users",
                column: "ParentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditTransactions_Users_SourceUserId",
                table: "CreditTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_CreditTransactions_Users_TargetUserId",
                table: "CreditTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogs_Users_UserId",
                table: "SessionLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Plans_PlanId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Plans_PlanId1",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId1",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ParentId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PlanId1",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PlanId1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId1",
                table: "Users");

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceUserId",
                table: "CreditTransactions",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditTransactions_Users_SourceUserId",
                table: "CreditTransactions",
                column: "SourceUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditTransactions_Users_TargetUserId",
                table: "CreditTransactions",
                column: "TargetUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogs_Users_UserId",
                table: "SessionLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Plans_PlanId",
                table: "Users",
                column: "PlanId",
                principalTable: "Plans",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ParentId",
                table: "Users",
                column: "ParentId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
