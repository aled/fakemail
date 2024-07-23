using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fakemail.Data.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentEmailTimestampToSmtpUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentEmailReceivedTimestampUtc",
                table: "SmtpUser",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentEmailReceivedTimestampUtc",
                table: "SmtpUser");
        }
    }
}
