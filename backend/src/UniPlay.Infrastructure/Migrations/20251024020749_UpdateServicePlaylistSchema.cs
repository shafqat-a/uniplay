using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniPlay.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateServicePlaylistSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "cover_image_url",
                schema: "public",
                table: "service_playlists",
                newName: "service_url");

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                schema: "public",
                table: "service_playlists",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "owner_name",
                schema: "public",
                table: "service_playlists",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_url",
                schema: "public",
                table: "service_playlists");

            migrationBuilder.DropColumn(
                name: "owner_name",
                schema: "public",
                table: "service_playlists");

            migrationBuilder.RenameColumn(
                name: "service_url",
                schema: "public",
                table: "service_playlists",
                newName: "cover_image_url");
        }
    }
}
