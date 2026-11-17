using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatWithMe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "role",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "conversation_members",
                columns: table => new
                {
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    joined_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_read_message_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    last_read_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    muted_until_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_members", x => new { x.conversation_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_conversation_members_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_conversation_members_user_id",
                table: "conversation_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversations_created_by_user_id",
                table: "conversations",
                column: "created_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversation_members");

            migrationBuilder.DropTable(
                name: "conversations");

            migrationBuilder.DropColumn(
                name: "role",
                table: "users");
        }
    }
}
