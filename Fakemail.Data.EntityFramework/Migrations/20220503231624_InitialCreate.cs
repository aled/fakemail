using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fakemail.Data.EntityFramework.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordCrypt = table.Column<string>(type: "TEXT", nullable: false),
                    InsertedTimestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastUpdatedTimestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DeletedTimestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Emails",
                columns: table => new
                {
                    EmailId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MimeMessage = table.Column<byte[]>(type: "BLOB", nullable: false),
                    From = table.Column<string>(type: "TEXT", nullable: false),
                    To = table.Column<string>(type: "TEXT", nullable: false),
                    CC = table.Column<string>(type: "TEXT", nullable: false),
                    DeliveredTo = table.Column<string>(type: "TEXT", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", nullable: false),
                    ReceivedFromHost = table.Column<string>(type: "TEXT", nullable: false),
                    ReceivedFromDns = table.Column<string>(type: "TEXT", nullable: false),
                    ReceivedFromIp = table.Column<string>(type: "TEXT", nullable: false),
                    ReceivedSmtpId = table.Column<string>(type: "TEXT", nullable: false),
                    ReceivedTlsInfo = table.Column<string>(type: "TEXT", nullable: false),
                    SmtpUser = table.Column<string>(type: "TEXT", nullable: false),
                    ReceivedTimestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    BodyLength = table.Column<int>(type: "INTEGER", nullable: false),
                    BodySummary = table.Column<string>(type: "TEXT", nullable: false),
                    BodyChecksum = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emails", x => x.EmailId);
                    table.ForeignKey(
                        name: "FK_Emails_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SmtpUsers",
                columns: table => new
                {
                    SmtpUsername = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SmtpPasswordCrypt = table.Column<string>(type: "TEXT", nullable: false),
                    InsertedTimestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastUpdatedTimestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DeletedTimestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmtpUsers", x => x.SmtpUsername);
                    table.ForeignKey(
                        name: "FK_SmtpUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    AttachmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmailId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Filename = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ContentChecksum = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.AttachmentId);
                    table.ForeignKey(
                        name: "FK_Attachments_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "EmailId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_EmailId",
                table: "Attachments",
                column: "EmailId");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_UserId",
                table: "Emails",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SmtpUsers_UserId",
                table: "SmtpUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "SmtpUsers");

            migrationBuilder.DropTable(
                name: "Emails");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
