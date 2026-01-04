using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductScrapperV2.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitNe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WebsiteHost",
                table: "Competitors",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WebsiteHost",
                table: "Competitors");
        }
    }
}
