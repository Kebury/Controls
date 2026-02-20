using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Controls.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrganizationName",
                table: "AppSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "Организация");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationName",
                table: "AppSettings");
        }
    }
}
