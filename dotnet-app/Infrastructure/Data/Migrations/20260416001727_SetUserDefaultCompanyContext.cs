using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceTracker.Web.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SetUserDefaultCompanyContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove legacy project data so CompanyId can be enforced as required.
            migrationBuilder.Sql("DELETE FROM [Transactions];");
            migrationBuilder.Sql("DELETE FROM [Projects];");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "UserCompanyMaps",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Projects",
                type: "int",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CompanyId",
                table: "Projects",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Companies_CompanyId",
                table: "Projects",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Companies_CompanyId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_CompanyId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "UserCompanyMaps");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Projects");
        }
    }
}
