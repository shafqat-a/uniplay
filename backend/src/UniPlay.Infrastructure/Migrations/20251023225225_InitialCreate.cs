using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniPlay.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "tracks",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    service_track_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    artist = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    album = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    isrc = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    image_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    service_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_accounts",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    password_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    security_stamp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "platform_playlists",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    track_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_playlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_platform_playlists_user_accounts_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "user_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_connections",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    service_account_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    service_account_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    service_account_display_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    service_account_profile_image_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    encrypted_access_token = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    encrypted_refresh_token = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    access_token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    scopes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    connection_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_connections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_connections_user_accounts_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "user_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_playlists",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_playlist_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    track_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    cover_image_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    is_owned_by_user = table.Column<bool>(type: "boolean", nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    is_collaborative = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    snapshot_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_playlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_playlists_service_connections_service_connection_id",
                        column: x => x.service_connection_id,
                        principalSchema: "public",
                        principalTable: "service_connections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "playlist_track_associations",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    playlist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    playlist_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    added_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playlist_track_associations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_playlist_track_associations_platform_playlists_playlist_id",
                        column: x => x.playlist_id,
                        principalSchema: "public",
                        principalTable: "platform_playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_playlist_track_associations_service_playlists_playlist_id",
                        column: x => x.playlist_id,
                        principalSchema: "public",
                        principalTable: "service_playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_playlist_track_associations_tracks_track_id",
                        column: x => x.track_id,
                        principalSchema: "public",
                        principalTable: "tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_platform_playlist_created",
                schema: "public",
                table: "platform_playlists",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_platform_playlist_name",
                schema: "public",
                table: "platform_playlists",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_platform_playlist_user",
                schema: "public",
                table: "platform_playlists",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_playlist_track_added",
                schema: "public",
                table: "playlist_track_associations",
                column: "added_at");

            migrationBuilder.CreateIndex(
                name: "idx_playlist_track_position",
                schema: "public",
                table: "playlist_track_associations",
                columns: new[] { "playlist_id", "playlist_type", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_playlist_track_track",
                schema: "public",
                table: "playlist_track_associations",
                column: "track_id");

            migrationBuilder.CreateIndex(
                name: "idx_service_connection_expiration",
                schema: "public",
                table: "service_connections",
                column: "access_token_expires_at",
                filter: "connection_status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "idx_service_connection_status",
                schema: "public",
                table: "service_connections",
                column: "connection_status");

            migrationBuilder.CreateIndex(
                name: "idx_service_connection_user_service",
                schema: "public",
                table: "service_connections",
                columns: new[] { "user_id", "service_type" });

            migrationBuilder.CreateIndex(
                name: "idx_service_playlist_connection_playlist",
                schema: "public",
                table: "service_playlists",
                columns: new[] { "service_connection_id", "service_playlist_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_service_playlist_last_synced",
                schema: "public",
                table: "service_playlists",
                column: "last_synced_at");

            migrationBuilder.CreateIndex(
                name: "idx_service_playlist_name",
                schema: "public",
                table: "service_playlists",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_track_isrc",
                schema: "public",
                table: "tracks",
                column: "isrc",
                filter: "isrc IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_track_service",
                schema: "public",
                table: "tracks",
                columns: new[] { "service_type", "service_track_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_created_at",
                schema: "public",
                table: "user_accounts",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_user_email",
                schema: "public",
                table: "user_accounts",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "playlist_track_associations",
                schema: "public");

            migrationBuilder.DropTable(
                name: "platform_playlists",
                schema: "public");

            migrationBuilder.DropTable(
                name: "service_playlists",
                schema: "public");

            migrationBuilder.DropTable(
                name: "tracks",
                schema: "public");

            migrationBuilder.DropTable(
                name: "service_connections",
                schema: "public");

            migrationBuilder.DropTable(
                name: "user_accounts",
                schema: "public");
        }
    }
}
