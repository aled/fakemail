using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fakemail.Data.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class CreateReceivedTimestampIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Email_ReceivedTimestampUtc",
                table: "Email",
                column: "ReceivedTimestampUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Email_ReceivedTimestampUtc",
                table: "Email");
        }
    }
}
