using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureFileStatementDelivery.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStatementaccountTypeAndPeriodKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "accountType",
                table: "Statements",
                type: "TEXT",
                nullable: false,
                defaultValue: "Main");

            migrationBuilder.AddColumn<int>(
                name: "PeriodKey",
                table: "Statements",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Backfill existing rows using the stored Period (expected format: YYYY-MM).
            migrationBuilder.Sql(
                "UPDATE Statements SET PeriodKey = (CAST(substr(Period, 1, 4) AS INTEGER) * 100) + CAST(substr(Period, 6, 2) AS INTEGER) " +
                "WHERE PeriodKey = 0 AND length(Period) = 7;");

            migrationBuilder.CreateIndex(
                name: "IX_Statements_CustomerId_AccountId_PeriodKey",
                table: "Statements",
                columns: new[] { "CustomerId", "AccountId", "PeriodKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Statements_CustomerId_accountType_PeriodKey",
                table: "Statements",
                columns: new[] { "CustomerId", "accountType", "PeriodKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Statements_CustomerId_AccountId_PeriodKey",
                table: "Statements");

            migrationBuilder.DropIndex(
                name: "IX_Statements_CustomerId_accountType_PeriodKey",
                table: "Statements");

            migrationBuilder.DropColumn(
                name: "accountType",
                table: "Statements");

            migrationBuilder.DropColumn(
                name: "PeriodKey",
                table: "Statements");
        }
    }
}
