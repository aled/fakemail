using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fakemail.Data.EntityFramework.Migrations
{
    public partial class ChangeDateTimeOffsetToDateTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedTimestamp",
                table: "User");

            migrationBuilder.DropColumn(
                name: "DeletedTimestamp",
                table: "SmtpUser");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedTimestamp",
                table: "User",
                newName: "UpdatedTimestampUtc");

            migrationBuilder.RenameColumn(
                name: "InsertedTimestamp",
                table: "User",
                newName: "CreatedTimestampUtc");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedTimestamp",
                table: "SmtpUser",
                newName: "UpdatedTimestampUtc");

            migrationBuilder.RenameColumn(
                name: "InsertedTimestamp",
                table: "SmtpUser",
                newName: "CreatedTimestampUtc");

            migrationBuilder.RenameColumn(
                name: "ReceivedTimestamp",
                table: "Email",
                newName: "ReceivedTimestampUtc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedTimestampUtc",
                table: "User",
                newName: "LastUpdatedTimestamp");

            migrationBuilder.RenameColumn(
                name: "CreatedTimestampUtc",
                table: "User",
                newName: "InsertedTimestamp");

            migrationBuilder.RenameColumn(
                name: "UpdatedTimestampUtc",
                table: "SmtpUser",
                newName: "LastUpdatedTimestamp");

            migrationBuilder.RenameColumn(
                name: "CreatedTimestampUtc",
                table: "SmtpUser",
                newName: "InsertedTimestamp");

            migrationBuilder.RenameColumn(
                name: "ReceivedTimestampUtc",
                table: "Email",
                newName: "ReceivedTimestamp");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedTimestamp",
                table: "User",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedTimestamp",
                table: "SmtpUser",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
