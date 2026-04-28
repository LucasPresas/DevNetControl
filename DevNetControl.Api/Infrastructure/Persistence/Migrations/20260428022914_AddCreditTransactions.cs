using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevNetControl.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ParentId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_VpsNode_Users_OwnerId",
                table: "VpsNode");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VpsNode",
                table: "VpsNode");

            migrationBuilder.RenameTable(
                name: "VpsNode",
                newName: "VpsNodes");

            migrationBuilder.RenameIndex(
                name: "IX_VpsNode_OwnerId",
                table: "VpsNodes",
                newName: "IX_VpsNodes_OwnerId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Credits",
                table: "Users",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VpsNodes",
                table: "VpsNodes",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "CreditTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditTransactions_Users_FromUserId",
                        column: x => x.FromUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditTransactions_Users_ToUserId",
                        column: x => x.ToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_FromUserId",
                table: "CreditTransactions",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_ToUserId",
                table: "CreditTransactions",
                column: "ToUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ParentId",
                table: "Users",
                column: "ParentId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VpsNodes_Users_OwnerId",
                table: "VpsNodes",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ParentId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_VpsNodes_Users_OwnerId",
                table: "VpsNodes");

            migrationBuilder.DropTable(
                name: "CreditTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VpsNodes",
                table: "VpsNodes");

            migrationBuilder.RenameTable(
                name: "VpsNodes",
                newName: "VpsNode");

            migrationBuilder.RenameIndex(
                name: "IX_VpsNodes_OwnerId",
                table: "VpsNode",
                newName: "IX_VpsNode_OwnerId");

            migrationBuilder.AlterColumn<int>(
                name: "Credits",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VpsNode",
                table: "VpsNode",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ParentId",
                table: "Users",
                column: "ParentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VpsNode_Users_OwnerId",
                table: "VpsNode",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
