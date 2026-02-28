using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Controls.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomDatesJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomDatesJson",
                table: "ControlTasks",
                type: "TEXT",
                maxLength: 5000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomDatesJson",
                table: "ControlTasks");
        }
    }
}
