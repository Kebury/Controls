using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Controls.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ControlTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TaskType = table.Column<int>(type: "INTEGER", nullable: false),
                    ImportancePriority = table.Column<int>(type: "INTEGER", nullable: false),
                    UrgencyPriority = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Assignee = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ControlDocumentPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ControlNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ReportTemplatePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ReportsFolderPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Executors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Position = table.Column<string>(type: "TEXT", nullable: false),
                    Rank = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Executors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AddedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    ControlTaskId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsTemplate = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_ControlTasks_ControlTaskId",
                        column: x => x.ControlTaskId,
                        principalTable: "ControlTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ControlTaskId = table.Column<int>(type: "INTEGER", nullable: false),
                    NotificationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotificationType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsNotified = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsReportSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    OutgoingNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    OutgoingDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsCompletedInWorkingOrder = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAwaitingReport = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOsNotificationSent = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_ControlTasks_ControlTaskId",
                        column: x => x.ControlTaskId,
                        principalTable: "ControlTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DepartmentTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaskNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    SentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ImportancePriority = table.Column<int>(type: "INTEGER", nullable: false),
                    ExecutorId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TaskFilePaths = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    DepartmentFilePaths = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IntermediateResponsesJson = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentTasks_Executors_ExecutorId",
                        column: x => x.ExecutorId,
                        principalTable: "Executors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DepartmentTaskDepartments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DepartmentTaskId = table.Column<int>(type: "INTEGER", nullable: false),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ExecutorId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentTaskDepartments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentTaskDepartments_DepartmentTasks_DepartmentTaskId",
                        column: x => x.DepartmentTaskId,
                        principalTable: "DepartmentTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DepartmentTaskDepartments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepartmentTaskDepartments_Executors_ExecutorId",
                        column: x => x.ExecutorId,
                        principalTable: "Executors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ControlTasks_DueDate",
                table: "ControlTasks",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_ControlTasks_Status",
                table: "ControlTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentTaskDepartments_DepartmentId",
                table: "DepartmentTaskDepartments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentTaskDepartments_DepartmentTaskId",
                table: "DepartmentTaskDepartments",
                column: "DepartmentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentTaskDepartments_ExecutorId",
                table: "DepartmentTaskDepartments",
                column: "ExecutorId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentTaskDepartments_IsCompleted",
                table: "DepartmentTaskDepartments",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentTasks_Department",
                table: "DepartmentTasks",
                column: "Department");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentTasks_DueDate",
                table: "DepartmentTasks",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentTasks_ExecutorId",
                table: "DepartmentTasks",
                column: "ExecutorId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentTasks_IsCompleted",
                table: "DepartmentTasks",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ControlTaskId",
                table: "Documents",
                column: "ControlTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ControlTaskId",
                table: "Notifications",
                column: "ControlTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead",
                table: "Notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_NotificationDate",
                table: "Notifications",
                column: "NotificationDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartmentTaskDepartments");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "DepartmentTasks");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "ControlTasks");

            migrationBuilder.DropTable(
                name: "Executors");
        }
    }
}
