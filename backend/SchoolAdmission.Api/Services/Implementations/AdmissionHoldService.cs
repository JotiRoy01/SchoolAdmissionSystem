
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Entities;
using SchoolAdmission.Api.Services.Interfaces;
using SchoolAdmission.Api.Services.Validation;

namespace SchoolAdmission.Api.Services.Implementations;

public class AdmissionHoldService : IAdmissionHoldService
{
    private readonly AdmissionDbContext _context;
    private readonly IConfiguration _configuration;

    public AdmissionHoldService(
        AdmissionDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    private int HoldDurationSeconds
    {
        get
        {
            var environmentValue =
                Environment.GetEnvironmentVariable("HOLD_DURATION_SECONDS");

            var configuredValue = int.TryParse(
                environmentValue,
                out var environmentSeconds)
                ? environmentSeconds
                : _configuration.GetValue<int?>(
                    "Admission:HoldDurationSeconds") ?? 600;

            return Math.Max(1, configuredValue);
        }
    }

    private static AdmissionHoldResponse MapToResponse(
        AdmissionHold hold)
    {
        return new AdmissionHoldResponse
        {
            Id = hold.Id,
            ClassId = hold.ClassId,
            ClassName = hold.Class.Name,
            StudentId = hold.Student.BirthRegistrationNumber,
            Status = hold.Status.ToString(),
            CreatedAt = hold.CreatedAt,
            ExpiresAt = hold.ExpiresAt,
            ConfirmedAt = hold.ConfirmedAt
            ,PaymentStatus = hold.PaymentStatus.ToString()
            ,PaidAt = hold.PaidAt
        };
    }

    // DATABASE OPERATION:
    // Lock the admission class until the transaction finishes.
    private async Task<AdmissionClass?> LockClassAsync(Guid classId)
    {
        return await _context.AdmissionClasses
            .FromSqlInterpolated(
                $"SELECT * FROM admission_classes WHERE id = {classId} FOR UPDATE")
            .FirstOrDefaultAsync();
    }

    // BUSINESS OPERATION:
    // Expire overdue holds belonging to a particular class.
    private async Task ExpireClassHoldsAsync(
        Guid classId,
        DateTime now)
    {
        await _context.AdmissionHolds
            .Where(h =>
                h.ClassId == classId &&
                h.Status == AdmissionHoldStatus.HELD &&
                h.ExpiresAt <= now)
            .ExecuteUpdateAsync(update => update
                .SetProperty(
                    h => h.Status,
                    AdmissionHoldStatus.EXPIRED));
    }

    // Find a hold and lock its associated class.
    private async Task<(AdmissionClass? Class, AdmissionHold? Hold)>
        GetLockedHoldAsync(Guid holdId)
    {
        var classId = await _context.AdmissionHolds
            .Where(h => h.Id == holdId)
            .Select(h => (Guid?)h.ClassId)
            .FirstOrDefaultAsync();

        if (classId == null)
            return (null, null);

        var admissionClass = await LockClassAsync(classId.Value);

        if (admissionClass == null)
            return (null, null);

        var hold = await _context.AdmissionHolds
            .Include(h => h.Class)
            .Include(h => h.Student)
            .FirstOrDefaultAsync(h => h.Id == holdId);

        return (admissionClass, hold);
    }

    // =========================================================
    // 1. CREATE HOLD
    // =========================================================

    public async Task<ServiceResult<AdmissionHoldResponse>>
        CreateHoldAsync(Guid classId, string? studentId)
    {
        if (!StudentIdValidator.IsValid(studentId))
        {
            return ServiceResult<AdmissionHoldResponse>.Failure(
                "A valid 17-digit birth registration number is required.",
                400);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Lock the admission class.
            var admissionClass = await LockClassAsync(classId);

            if (admissionClass == null)
            {
                await transaction.CommitAsync();

                return ServiceResult<AdmissionHoldResponse>.Failure(
                    "Admission class not found.",
                    404);
            }

            var now = DateTime.UtcNow;

            // 2. Expire overdue holds before checking capacity.
            await ExpireClassHoldsAsync(classId, now);

            // 3. Check for an existing active or confirmed application.
            var existingHold = await _context.AdmissionHolds
                .Include(h => h.Class)
                .Include(h => h.Student)
                .FirstOrDefaultAsync(h =>
                    h.ClassId == classId &&
                    h.Student.BirthRegistrationNumber == studentId &&
                    (h.Status == AdmissionHoldStatus.HELD ||
                     h.Status == AdmissionHoldStatus.CONFIRMED));

            if (existingHold != null)
            {
                var message =
                    existingHold.Status == AdmissionHoldStatus.CONFIRMED
                        ? "You already have a confirmed application for this class."
                        : "You already have an active reservation for this class.";

                // Commit so that any expired holds remain expired.
                await transaction.CommitAsync();

                return ServiceResult<AdmissionHoldResponse>.Failure(
                    message,
                    409);
            }

            // 4. Calculate occupied seats.
            var occupiedSeats = await _context.AdmissionHolds
                .CountAsync(h =>
                    h.ClassId == classId &&
                    (h.Status == AdmissionHoldStatus.HELD ||
                     h.Status == AdmissionHoldStatus.CONFIRMED));

            if (occupiedSeats >= admissionClass.TotalSeats)
            {
                await transaction.CommitAsync();

                return ServiceResult<AdmissionHoldResponse>.Failure(
                    "Sorry, no seats are currently available.",
                    409);
            }

            // 5. Find or create the student.
            var student = await _context.Students
                .FirstOrDefaultAsync(s =>
                    s.BirthRegistrationNumber == studentId);

            if (student == null)
            {
                student = new Student
                {
                    BirthRegistrationNumber = studentId!,
                    CreatedAt = now
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();
            }

            // 6. Create the temporary hold.
            var hold = new AdmissionHold
            {
                ClassId = classId,
                StudentId = student.Id,
                Status = AdmissionHoldStatus.HELD,
                CreatedAt = now,
                ExpiresAt = now.AddSeconds(HoldDurationSeconds)
            };

            _context.AdmissionHolds.Add(hold);

            await _context.SaveChangesAsync();

            // 7. Commit the reservation.
            await transaction.CommitAsync();

            hold.Class = admissionClass;
            hold.Student = student;

            return ServiceResult<AdmissionHoldResponse>.Success(
                MapToResponse(hold),
                201);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException postgresException &&
                  postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync();

            return ServiceResult<AdmissionHoldResponse>.Failure(
                "A conflicting student or application record already exists. Please try again.",
                409);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // =========================================================
    // 2. GET HOLD BY ID
    // =========================================================

    public async Task<ServiceResult<AdmissionHoldResponse>>
        GetHoldByIdAsync(Guid holdId)
    {
        var now = DateTime.UtcNow;

        // Lazy expiration safeguard.
        await _context.AdmissionHolds
            .Where(h =>
                h.Id == holdId &&
                h.Status == AdmissionHoldStatus.HELD &&
                h.ExpiresAt <= now)
            .ExecuteUpdateAsync(update => update
                .SetProperty(
                    h => h.Status,
                    AdmissionHoldStatus.EXPIRED));

        var hold = await _context.AdmissionHolds
            .AsNoTracking()
            .Include(h => h.Class)
            .Include(h => h.Student)
            .FirstOrDefaultAsync(h => h.Id == holdId);

        if (hold == null)
        {
            return ServiceResult<AdmissionHoldResponse>.Failure(
                "Reservation not found.",
                404);
        }

        return ServiceResult<AdmissionHoldResponse>.Success(
            MapToResponse(hold));
    }

    // =========================================================
    // 3. CONFIRM HOLD
    // =========================================================

        
    public async Task<ServiceResult<AdmissionHoldResponse>>
        ConfirmHoldAsync(Guid holdId, string? studentId)
    {
        // 1. Validate the student's birth registration number.
        if (!StudentIdValidator.IsValid(studentId))
        {
            return ServiceResult<AdmissionHoldResponse>.Failure(
                "A valid 17-digit birth registration number is required.",
                400);
        }

        // 2. Start a database transaction.
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // 3. Lock the admission class and retrieve the hold.
            // This prevents concurrent operations from changing
            // the same class's reservation state unexpectedly.
            var (_, hold) = await GetLockedHoldAsync(holdId);

            if (hold == null)
            {
                await transaction.CommitAsync();

                return ServiceResult<AdmissionHoldResponse>.Failure(
                    "Reservation not found.",
                    404);
            }

            // 4. Verify that this reservation belongs to the student.
            if (hold.Student.BirthRegistrationNumber != studentId)
            {
                await transaction.CommitAsync();

                return ServiceResult<AdmissionHoldResponse>.Failure(
                    "You are not authorized to confirm this reservation.",
                    403);
            }

            var now = DateTime.UtcNow;

            // 5. Expire overdue holds for this admission class.
            await ExpireClassHoldsAsync(hold.ClassId, now);

            // ExecuteUpdateAsync does not update EF's tracked entity.
            // Reload to obtain the latest database status.
            await _context.Entry(hold).ReloadAsync();

            // 6. Idempotency:
            // If the application is already confirmed, return success.
            if (hold.Status == AdmissionHoldStatus.CONFIRMED)
            {
                await transaction.CommitAsync();

                return ServiceResult<AdmissionHoldResponse>.Success(
                    MapToResponse(hold));
            }

            // 7. The reservation must still be HELD.
            if (hold.Status != AdmissionHoldStatus.HELD)
            {
                var currentStatus = hold.Status;

                await transaction.CommitAsync();

                return ServiceResult<AdmissionHoldResponse>.Failure(
                    $"This reservation cannot be confirmed because its status is {currentStatus}.",
                    409);
            }

            // 8. Final expiry check.
            if (hold.ExpiresAt <= now)
            {
                hold.Status = AdmissionHoldStatus.EXPIRED;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ServiceResult<AdmissionHoldResponse>.Failure(
                    "Your reservation has expired. Please create a new reservation if seats are available.",
                    409);
            }

            // 9. Simulate successful admission-fee payment.
            hold.PaymentStatus = AdmissionPaymentStatus.PAID;
            hold.PaidAt = now;

            // 10. Confirm the admission.
            hold.Status = AdmissionHoldStatus.CONFIRMED;
            hold.ConfirmedAt = now;

            // 11. Save payment and confirmation together.
            await _context.SaveChangesAsync();

            // 12. Commit the transaction.
            await transaction.CommitAsync();

            return ServiceResult<AdmissionHoldResponse>.Success(
                MapToResponse(hold));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }


    //<=========PayAdmissionFeeAsync===============>

    // public async Task<ServiceResult<AdmissionHoldResponse>>
    //     PayAdmissionFeeAsync(Guid holdId, string? studentId)
    // {
    //     if (!StudentIdValidator.IsValid(studentId))
    //     {
    //         return ServiceResult<AdmissionHoldResponse>.Failure(
    //             "A valid 17-digit birth registration number is required.",
    //             400);
    //     }

    //     await using var transaction =
    //         await _context.Database.BeginTransactionAsync();

    //     try
    //     {
    //         var (_, hold) = await GetLockedHoldAsync(holdId);

    //         if (hold == null)
    //         {
    //             await transaction.CommitAsync();
    //             return ServiceResult<AdmissionHoldResponse>.Failure(
    //                 "Reservation not found.", 404);
    //         }

    //         if (hold.Student.BirthRegistrationNumber != studentId)
    //         {
    //             await transaction.CommitAsync();
    //             return ServiceResult<AdmissionHoldResponse>.Failure(
    //                 "You are not authorized to pay for this reservation.", 403);
    //         }

    //         if (hold.PaymentStatus == AdmissionPaymentStatus.PAID)
    //         {
    //             await transaction.CommitAsync();
    //             return ServiceResult<AdmissionHoldResponse>.Success(
    //                 MapToResponse(hold));
    //         }

    //         var now = DateTime.UtcNow;
    //         await ExpireClassHoldsAsync(hold.ClassId, now);
    //         await _context.Entry(hold).ReloadAsync();

    //         if (hold.Status != AdmissionHoldStatus.HELD)
    //         {
    //             await transaction.CommitAsync();
    //             return ServiceResult<AdmissionHoldResponse>.Failure(
    //                 "Payment is unavailable because this reservation is no longer active.",
    //                 409);
    //         }

    //         if (hold.ExpiresAt <= now)
    //         {
    //             hold.Status = AdmissionHoldStatus.EXPIRED;
    //             await _context.SaveChangesAsync();
    //             await transaction.CommitAsync();
    //             return ServiceResult<AdmissionHoldResponse>.Failure(
    //                 "Your reservation has expired. Please create a new reservation.",
    //                 409);
    //         }

    //         hold.PaymentStatus = AdmissionPaymentStatus.PAID;
    //         hold.PaidAt = now;
    //         await _context.SaveChangesAsync();
    //         await transaction.CommitAsync();

    //         return ServiceResult<AdmissionHoldResponse>.Success(
    //             MapToResponse(hold));
    //     }
    //     catch
    //     {
    //         await transaction.RollbackAsync();
    //         throw;
    //     }
    // }

    // =========================================================
    // 4. CANCEL HOLD
    // =========================================================

    public async Task<ServiceResult<AdmissionHoldResponse>>
        CancelHoldAsync(Guid holdId, string? studentId)
    {
        if (!StudentIdValidator.IsValid(studentId))
        {
            return ServiceResult<AdmissionHoldResponse>.Failure(
                "A valid 17-digit birth registration number is required.",
                400);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var (_, hold) = await GetLockedHoldAsync(holdId);

            if (hold == null)
            {
                await transaction.CommitAsync();

                return ServiceResult<AdmissionHoldResponse>.Failure(
                    "Reservation not found.",
                    404);
            }

            // Verify ownership.
            if (hold.Student.BirthRegistrationNumber != studentId)
            {
                await transaction.CommitAsync();

                return ServiceResult<AdmissionHoldResponse>.Failure(
                    "You are not authorized to cancel this reservation.",
                    403);
            }

            var now = DateTime.UtcNow;

            await ExpireClassHoldsAsync(hold.ClassId, now);

            // Reload after bulk expiration.
            await _context.Entry(hold).ReloadAsync();

            // Idempotency: repeated cancellation returns
            // the existing cancelled reservation.
            if (hold.Status == AdmissionHoldStatus.CANCELLED)
            {
                await transaction.CommitAsync();

                return ServiceResult<AdmissionHoldResponse>.Success(
                    MapToResponse(hold));
            }

            // Do not allow cancellation of an expired hold.
            if (hold.Status == AdmissionHoldStatus.HELD &&
                hold.ExpiresAt <= now)
            {
                hold.Status = AdmissionHoldStatus.EXPIRED;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ServiceResult<AdmissionHoldResponse>.Failure(
                    "This reservation has expired and cannot be cancelled.",
                    409);
            }

            if (hold.Status != AdmissionHoldStatus.HELD)
            {
                var currentStatus = hold.Status;

                await transaction.CommitAsync();

                return ServiceResult<AdmissionHoldResponse>.Failure(
                    $"A reservation with status {currentStatus} cannot be cancelled.",
                    409);
            }

            hold.Status = AdmissionHoldStatus.CANCELLED;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<AdmissionHoldResponse>.Success(
                MapToResponse(hold));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // =========================================================
    // 5. RESTORE STUDENT APPLICATIONS
    // =========================================================

    public async Task<ServiceResult<List<AdmissionHoldResponse>>>
        GetMyHoldsAsync(string? studentId)
    {
        if (!StudentIdValidator.IsValid(studentId))
        {
            return ServiceResult<List<AdmissionHoldResponse>>.Failure(
                "A valid 17-digit birth registration number is required.",
                400);
        }

        var now = DateTime.UtcNow;

        // Expire this student's overdue holds before returning history.
        await _context.AdmissionHolds
            .Where(h =>
                h.Student.BirthRegistrationNumber == studentId &&
                h.Status == AdmissionHoldStatus.HELD &&
                h.ExpiresAt <= now)
            .ExecuteUpdateAsync(update => update
                .SetProperty(
                    h => h.Status,
                    AdmissionHoldStatus.EXPIRED));

        var holds = await _context.AdmissionHolds
            .AsNoTracking()
            .Include(h => h.Class)
            .Include(h => h.Student)
            .Where(h => h.Student.BirthRegistrationNumber == studentId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        var response = holds
            .Select(MapToResponse)
            .ToList();

        return ServiceResult<List<AdmissionHoldResponse>>.Success(
            response);
    }

    // =========================================================
    // 6. BACKGROUND WORKER EXPIRATION OPERATION
    // =========================================================

    public async Task<int> ExpireOverdueHoldsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var expiredCount = await _context.AdmissionHolds
            .Where(h =>
                h.Status == AdmissionHoldStatus.HELD &&
                h.ExpiresAt <= now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    h => h.Status,
                    AdmissionHoldStatus.EXPIRED),
                cancellationToken);

        return expiredCount;
    }
}
