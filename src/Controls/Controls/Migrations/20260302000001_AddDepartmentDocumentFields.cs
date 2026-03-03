using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Controls.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentDocumentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Номер направленного документа для каждой организации
            migrationBuilder.AddColumn<string>(
                name: "OutgoingDocumentNumber",
                table: "DepartmentTaskDepartments",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            // Пути к направленным файлам (исходящие) для каждой организации
            migrationBuilder.AddColumn<string>(
                name: "OutgoingFilePaths",
                table: "DepartmentTaskDepartments",
                type: "TEXT",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            // Пути к поступившим файлам (входящие) от каждой организации
            migrationBuilder.AddColumn<string>(
                name: "IncomingFilePaths",
                table: "DepartmentTaskDepartments",
                type: "TEXT",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OutgoingDocumentNumber",
                table: "DepartmentTaskDepartments");

            migrationBuilder.DropColumn(
                name: "OutgoingFilePaths",
                table: "DepartmentTaskDepartments");

            migrationBuilder.DropColumn(
                name: "IncomingFilePaths",
                table: "DepartmentTaskDepartments");
        }
    }
}
