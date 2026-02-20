using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Controls.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExecutorRank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rank",
                table: "Executors");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Rank",
                table: "Executors",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
