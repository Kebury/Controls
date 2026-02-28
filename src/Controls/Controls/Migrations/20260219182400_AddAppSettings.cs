using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Controls.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    CheckIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    DueTomorrowIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    DueTodayIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    OverdueIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AppSettings",
                columns: new[] { "Id", "CheckIntervalMinutes", "DueTodayIntervalMinutes", "DueTomorrowIntervalMinutes", "OverdueIntervalMinutes" },
                values: new object[] { 1, 30, 30, 720, 15 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");
        }
    }
}
