using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatWithMe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "file_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    stored_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    relative_path = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    size_in_bytes = table.Column<long>(type: "bigint", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_assets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    nick_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_file_assets_created_at_utc",
                table: "file_assets",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_file_assets_stored_name",
                table: "file_assets",
                column: "stored_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_created_at_utc",
                table: "users",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_assets");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
