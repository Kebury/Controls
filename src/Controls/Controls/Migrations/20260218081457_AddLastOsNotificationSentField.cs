using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Controls.Migrations
{
    /// <inheritdoc />
    public partial class AddLastOsNotificationSentField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastOsNotificationSent",
                table: "Notifications",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastOsNotificationSent",
                table: "Notifications");
        }
    }
}
