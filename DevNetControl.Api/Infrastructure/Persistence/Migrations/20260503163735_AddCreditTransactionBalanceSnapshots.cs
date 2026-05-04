using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevNetControl.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditTransactionBalanceSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SourceBalanceAfter",
                table: "CreditTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SourceBalanceBefore",
                table: "CreditTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetBalanceAfter",
                table: "CreditTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetBalanceBefore",
                table: "CreditTransactions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceBalanceAfter",
                table: "CreditTransactions");

            migrationBuilder.DropColumn(
                name: "SourceBalanceBefore",
                table: "CreditTransactions");

            migrationBuilder.DropColumn(
                name: "TargetBalanceAfter",
                table: "CreditTransactions");

            migrationBuilder.DropColumn(
                name: "TargetBalanceBefore",
                table: "CreditTransactions");
        }
    }
}
