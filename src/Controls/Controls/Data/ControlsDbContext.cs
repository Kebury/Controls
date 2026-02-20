using Microsoft.EntityFrameworkCore;
using Controls.Models;
using System;
using System.IO;

namespace Controls.Data
{
    public class ControlsDbContext : DbContext
    {
        public DbSet<ControlTask> ControlTasks { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<Executor> Executors { get; set; } = null!;
        public DbSet<DepartmentTask> DepartmentTasks { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<DepartmentTaskDepartment> DepartmentTaskDepartments { get; set; } = null!;
        public DbSet<AppSettings> AppSettings { get; set; } = null!;

        public ControlsDbContext() : base()
        {
        }

        public ControlsDbContext(DbContextOptions<ControlsDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var dbPath = DatabaseConfiguration.GetDatabasePath();
                
                var connectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
                {
                    DataSource = dbPath,
                    Cache = Microsoft.Data.Sqlite.SqliteCacheMode.Shared,
                    Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadWriteCreate,
                    DefaultTimeout = 60
                };
                
                optionsBuilder.UseSqlite(connectionString.ToString(), options =>
                {
                    options.CommandTimeout(120);
                });
                
                optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
                
                optionsBuilder.EnableSensitiveDataLogging(false);
                optionsBuilder.EnableDetailedErrors(true);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.ControlTask)
                .WithMany(ct => ct.Documents)
                .HasForeignKey(d => d.ControlTaskId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.ControlTask)
                .WithMany()
                .HasForeignKey(n => n.ControlTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ControlTask>()
                .HasIndex(ct => ct.DueDate);

            modelBuilder.Entity<ControlTask>()
                .HasIndex(ct => ct.Status);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.NotificationDate);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.IsRead);

            modelBuilder.Entity<DepartmentTask>()
                .HasIndex(dt => dt.DueDate);

            modelBuilder.Entity<DepartmentTask>()
                .HasIndex(dt => dt.IsCompleted);

            modelBuilder.Entity<DepartmentTask>()
                .HasIndex(dt => dt.Department);

            modelBuilder.Entity<DepartmentTaskDepartment>()
                .HasOne(dtd => dtd.DepartmentTask)
                .WithMany(dt => dt.TaskDepartments)
                .HasForeignKey(dtd => dtd.DepartmentTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DepartmentTaskDepartment>()
                .HasOne(dtd => dtd.Department)
                .WithMany()
                .HasForeignKey(dtd => dtd.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DepartmentTaskDepartment>()
                .HasIndex(dtd => dtd.DepartmentTaskId);

            modelBuilder.Entity<DepartmentTaskDepartment>()
                .HasIndex(dtd => dtd.DepartmentId);

            modelBuilder.Entity<DepartmentTaskDepartment>()
                .HasIndex(dtd => dtd.IsCompleted);

            modelBuilder.Entity<AppSettings>()
                .HasData(new AppSettings { Id = 1 });
        }

        /// <summary>
        /// Получение настроек приложения (singleton). Создает запись, если её нет.
        /// </summary>
        public AppSettings GetAppSettings()
        {
            var settings = AppSettings.AsTracking().FirstOrDefault();
            if (settings == null)
            {
                settings = new AppSettings { Id = 1 };
                AppSettings.Add(settings);
                SaveChanges();
            }
            return settings;
        }
    }
}
