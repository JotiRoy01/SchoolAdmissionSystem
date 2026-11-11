
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Entities;

namespace SchoolAdmission.Api.Data;

public class AdmissionDbContext : DbContext
{
    public AdmissionDbContext(DbContextOptions<AdmissionDbContext> options)
        : base(options)
    {
    }

    public DbSet<AdmissionClass> AdmissionClasses => Set<AdmissionClass>();

    public DbSet<Student> Students => Set<Student>();

    public DbSet<AdmissionHold> AdmissionHolds => Set<AdmissionHold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        
        // ADMISSION CLASS CONFIGURATION
      

        modelBuilder.Entity<AdmissionClass>(entity =>
        {
            entity.ToTable("admission_classes", table =>
            {
                table.HasCheckConstraint(
                    "ck_admission_classes_total_seats",
                    "\"total_seats\" > 0");
            });

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                .HasColumnName("id");

            entity.Property(c => c.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(c => c.TotalSeats)
                .HasColumnName("total_seats")
                .IsRequired();

            entity.Property(c => c.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();
        });

        // STUDENT CONFIGURATION

        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("students", table =>
            {
                table.HasCheckConstraint(
                    "ck_students_birth_registration_number",
                    "\"birth_registration_number\" ~ '^[0-9]{17}$'");
            });

            entity.HasKey(s => s.Id);

            entity.Property(s => s.Id)
                .HasColumnName("id");

            entity.Property(s => s.BirthRegistrationNumber)
                .HasColumnName("birth_registration_number")
                .HasMaxLength(17)
                .IsRequired();

            entity.Property(s => s.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.HasIndex(s => s.BirthRegistrationNumber)
                .IsUnique()
                .HasDatabaseName("ux_students_birth_registration_number");
        });


        // ADMISSION HOLD CONFIGURATION

        modelBuilder.Entity<AdmissionHold>(entity =>
        {
            entity.ToTable("admission_holds", table =>
            {
                table.HasCheckConstraint(
                    "ck_admission_holds_status",
                    "\"status\" IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')");
            });

            entity.HasKey(h => h.Id);

            entity.Property(h => h.Id)
                .HasColumnName("id");

            entity.Property(h => h.ClassId)
                .HasColumnName("class_id")
                .IsRequired();

            entity.Property(h => h.StudentId)
                .HasColumnName("student_id")
                .IsRequired();

            entity.Property(h => h.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(h => h.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(h => h.ExpiresAt)
                .HasColumnName("expires_at")
                .IsRequired();

            entity.Property(h => h.ConfirmedAt)
                .HasColumnName("confirmed_at");

            entity.Property(h => h.PaymentStatus)
                .HasColumnName("payment_status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(h => h.PaidAt)
                .HasColumnName("paid_at");

            // A class can have many admission holds.
            entity.HasOne(h => h.Class)
                .WithMany(c => c.Holds)
                .HasForeignKey(h => h.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // A student can have many admission holds.
            entity.HasOne(h => h.Student)
                .WithMany(s => s.Holds)
                .HasForeignKey(h => h.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Supports counting holds belonging to a class.
            entity.HasIndex(h => new { h.ClassId, h.Status })
                .HasDatabaseName("ix_admission_holds_class_status");

            // Supports finding expired holds efficiently.
            entity.HasIndex(h => h.ExpiresAt)
                .HasDatabaseName("ix_admission_holds_expires_at")
                .HasFilter("\"status\" = 'HELD'");

            // A student cannot have multiple HELD or CONFIRMED
            // applications for the same class.
            entity.HasIndex(h => new { h.StudentId, h.ClassId })
                .IsUnique()
                .HasDatabaseName("ux_active_student_class")
                .HasFilter("\"status\" IN ('HELD', 'CONFIRMED')");
        });
    }
}
