using Attendance.Domain;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Infrastructure.Persistence;

internal sealed class AttendanceDbContext : DbContext
{
    public const string UniqueAttendanceIndex = "IX_AttendanceRecords_Student_Date";
    public const string OpenAlertIndex = "IX_StudentAlerts_Open";

    public AttendanceDbContext(DbContextOptions<AttendanceDbContext> options) : base(options)
    {
    }

    public DbSet<School> Schools => Set<School>();

    public DbSet<Student> Students => Set<Student>();

    public DbSet<AttendanceCode> AttendanceCodes => Set<AttendanceCode>();

    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    public DbSet<StudentAlert> StudentAlerts => Set<StudentAlert>();

    public DbSet<AttendanceSubmission> AttendanceSubmissions => Set<AttendanceSubmission>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<School>(e =>
        {
            e.ToTable("Schools");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        b.Entity<Student>(e =>
        {
            e.ToTable("Students");
            e.HasKey(x => x.Id);
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Grade).HasMaxLength(10);
            e.HasOne<School>().WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.SchoolId, x.Grade });
        });

        b.Entity<AttendanceCode>(e =>
        {
            e.ToTable("AttendanceCodes");
            e.HasKey(x => x.Value);
            e.Property(x => x.Value).HasMaxLength(AttendanceCode.MaxValueLength);
            e.Property(x => x.Description).HasMaxLength(100).IsRequired();
        });

        b.Entity<AttendanceRecord>(e =>
        {
            e.ToTable("AttendanceRecords");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(AttendanceCode.MaxValueLength).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(AttendanceRecord.MaxNotesLength);
            e.Property(x => x.CreatedBy).HasMaxLength(100).IsRequired();
            e.Property(x => x.ModifiedBy).HasMaxLength(100);
            e.HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<School>().WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AttendanceCode>().WithMany().HasForeignKey(x => x.Code).OnDelete(DeleteBehavior.Restrict);

            // The one-per-student-per-day rule is the index, not a read-then-write check.
            e.HasIndex(x => new { x.StudentId, x.Date }).IsUnique().HasDatabaseName(UniqueAttendanceIndex);
            e.HasIndex(x => new { x.SchoolId, x.Date });
        });

        b.Entity<StudentAlert>(e =>
        {
            e.ToTable("StudentAlerts");
            e.HasKey(x => x.Id);
            e.Property(x => x.AlertType).HasMaxLength(50).IsRequired();
            e.Property(x => x.SchoolYear).HasMaxLength(9).IsRequired();
            e.Property(x => x.Message).HasMaxLength(500).IsRequired();
            e.Property(x => x.ResolvedBy).HasMaxLength(100);
            e.HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<School>().WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.StudentId, x.SchoolYear, x.AlertType })
                .IsUnique()
                .HasFilter("\"ResolvedAt\" IS NULL")
                .HasDatabaseName(OpenAlertIndex);
        });

        b.Entity<AttendanceSubmission>(e =>
        {
            e.ToTable("AttendanceSubmissions");
            e.HasKey(x => x.Id);
            e.Property(x => x.SubmittedBy).HasMaxLength(100).IsRequired();
            e.HasOne<School>().WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.SchoolId, x.Date });
        });
    }
}
