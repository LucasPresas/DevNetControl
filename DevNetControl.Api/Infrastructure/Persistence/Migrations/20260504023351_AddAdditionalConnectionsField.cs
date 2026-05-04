using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevNetControl.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdditionalConnectionsField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdditionalConnections",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalConnections",
                table: "Users");
        }
    }
}
