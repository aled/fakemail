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
                name: "SmtpAlias",
                columns: table => new
                {
                    Account = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmtpAlias", x => x.Account);
                });

            migrationBuilder.CreateTable(
                name: "User",
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
                    table.PrimaryKey("PK_User", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "SmtpUser",
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
                    table.PrimaryKey("PK_SmtpUser", x => x.SmtpUsername);
                    table.ForeignKey(
                        name: "FK_SmtpUser_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Email",
                columns: table => new
                {
                    EmailId = table.Column<Guid>(type: "TEXT", nullable: false),
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
                    ReceivedTimestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    BodyLength = table.Column<int>(type: "INTEGER", nullable: false),
                    BodySummary = table.Column<string>(type: "TEXT", nullable: false),
                    BodyChecksum = table.Column<int>(type: "INTEGER", nullable: false),
                    SmtpUsername = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Email", x => x.EmailId);
                    table.ForeignKey(
                        name: "FK_Email_SmtpUser_SmtpUsername",
                        column: x => x.SmtpUsername,
                        principalTable: "SmtpUser",
                        principalColumn: "SmtpUsername");
                });

            migrationBuilder.CreateTable(
                name: "Attachment",
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
                    table.PrimaryKey("PK_Attachment", x => x.AttachmentId);
                    table.ForeignKey(
                        name: "FK_Attachment_Email_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Email",
                        principalColumn: "EmailId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "SmtpAlias",
                column: "Account",
                value: "fakemail");

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_EmailId",
                table: "Attachment",
                column: "EmailId");

            migrationBuilder.CreateIndex(
                name: "IX_Email_SmtpUsername",
                table: "Email",
                column: "SmtpUsername");

            migrationBuilder.CreateIndex(
                name: "IX_SmtpUser_UserId",
                table: "SmtpUser",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_User_Username",
                table: "User",
                column: "Username",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attachment");

            migrationBuilder.DropTable(
                name: "SmtpAlias");

            migrationBuilder.DropTable(
                name: "Email");

            migrationBuilder.DropTable(
                name: "SmtpUser");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
