using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceEvent.Migrations
{
    /// <inheritdoc />
    public partial class AddImagesToDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerBlobName",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerImageUrl",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerBlobName",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "BannerImageUrl",
                table: "Events");
        }
    }
}
