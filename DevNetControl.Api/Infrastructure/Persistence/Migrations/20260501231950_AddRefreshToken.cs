using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevNetControl.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditTransactions_Tenants_TenantId",
                table: "CreditTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_CreditTransactions_Users_FromUserId",
                table: "CreditTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_CreditTransactions_Users_ToUserId",
                table: "CreditTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CreditTransactions_FromUserId",
                table: "CreditTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CreditTransactions_TenantId",
                table: "CreditTransactions");

            migrationBuilder.DropColumn(
                name: "FromUserId",
                table: "CreditTransactions");

            migrationBuilder.RenameColumn(
                name: "ToUserId",
                table: "CreditTransactions",
                newName: "SourceUserId");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "CreditTransactions",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_CreditTransactions_ToUserId",
                table: "CreditTransactions",
                newName: "IX_CreditTransactions_SourceUserId");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "CreditTransactions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "TargetUserId",
                table: "CreditTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsUsed = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsRevoked = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_TargetUserId",
                table: "CreditTransactions",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

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

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_CreditTransactions_TargetUserId",
                table: "CreditTransactions");

            migrationBuilder.DropColumn(
                name: "TargetUserId",
                table: "CreditTransactions");

            migrationBuilder.RenameColumn(
                name: "SourceUserId",
                table: "CreditTransactions",
                newName: "ToUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "CreditTransactions",
                newName: "Timestamp");

            migrationBuilder.RenameIndex(
                name: "IX_CreditTransactions_SourceUserId",
                table: "CreditTransactions",
                newName: "IX_CreditTransactions_ToUserId");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "CreditTransactions",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FromUserId",
                table: "CreditTransactions",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_FromUserId",
                table: "CreditTransactions",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_TenantId",
                table: "CreditTransactions",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditTransactions_Tenants_TenantId",
                table: "CreditTransactions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
        }
    }
}
