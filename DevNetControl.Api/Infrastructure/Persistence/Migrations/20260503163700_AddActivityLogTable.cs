using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevNetControl.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActionType = table.Column<int>(type: "INTEGER", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActorUserName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ActorRole = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TargetUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TargetUserName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreditsConsumed = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreditsBalanceBefore = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreditsBalanceAfter = table.Column<decimal>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Details = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    PlanId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PlanName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NodeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    NodeLabel = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_VpsNodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "VpsNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_ActorUserId",
                table: "ActivityLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_NodeId",
                table: "ActivityLogs",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_PlanId",
                table: "ActivityLogs",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_TargetUserId",
                table: "ActivityLogs",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_TenantId",
                table: "ActivityLogs",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");
        }
    }
}
