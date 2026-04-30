using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevNetControl.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorMultiTenancyFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_PlanAccesses_PlanId_UserId",
                table: "PlanAccesses");

            migrationBuilder.DropIndex(
                name: "IX_NodeAccesses_NodeId_UserId",
                table: "NodeAccesses");

            migrationBuilder.CreateIndex(
                name: "IX_PlanAccesses_PlanId",
                table: "PlanAccesses",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_NodeAccesses_NodeId",
                table: "NodeAccesses",
                column: "NodeId");

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
                name: "FK_Users_Tenants_TenantId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_VpsNodes_Tenants_TenantId",
                table: "VpsNodes");

            migrationBuilder.DropIndex(
                name: "IX_PlanAccesses_PlanId",
                table: "PlanAccesses");

            migrationBuilder.DropIndex(
                name: "IX_NodeAccesses_NodeId",
                table: "NodeAccesses");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Subdomain",
                table: "Tenants",
                column: "Subdomain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanAccesses_PlanId_UserId",
                table: "PlanAccesses",
                columns: new[] { "PlanId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NodeAccesses_NodeId_UserId",
                table: "NodeAccesses",
                columns: new[] { "NodeId", "UserId" },
                unique: true);

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
