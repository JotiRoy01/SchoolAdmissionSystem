using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Entities;
using SchoolAdmission.Api.Services.Interfaces;

namespace SchoolAdmission.Api.Services.Implementations;

public class AdmissionClassService : IAdmissionClassService
{
    private readonly AdmissionDbContext _context;

    public AdmissionClassService(AdmissionDbContext context)
    {
        _context = context;
    }

    private static AdmissionClassResponse MapToResponse(
        AdmissionClass admissionClass,
        int confirmedSeats,
        int heldSeats)
    {
        return new AdmissionClassResponse
        {
            Id = admissionClass.Id,
            Name = admissionClass.Name,
            TotalSeats = admissionClass.TotalSeats,
            ConfirmedSeats = confirmedSeats,
            HeldSeats = heldSeats,
            AvailableSeats = admissionClass.TotalSeats
                - confirmedSeats
                - heldSeats,
            CreatedAt = admissionClass.CreatedAt
        };
    }

    // Retrieve seat counts for one or more classes in a single query.
    private async Task<Dictionary<Guid, (int Confirmed, int Held)>>
        GetSeatCountsAsync(DateTime now, Guid? classId = null)
    {
        var query = _context.AdmissionHolds
            .AsNoTracking()
            .Where(h =>
                h.Status == AdmissionHoldStatus.CONFIRMED ||
                (h.Status == AdmissionHoldStatus.HELD &&
                 h.ExpiresAt > now));

        if (classId.HasValue)
        {
            query = query.Where(h => h.ClassId == classId.Value);
        }

        return await query
            .GroupBy(h => h.ClassId)
            .Select(g => new
            {
                ClassId = g.Key,
                Confirmed = g.Count(
                    h => h.Status == AdmissionHoldStatus.CONFIRMED),
                Held = g.Count(
                    h => h.Status == AdmissionHoldStatus.HELD)
            })
            .ToDictionaryAsync(
                x => x.ClassId,
                x => (x.Confirmed, x.Held));
    }

    // CREATE CLASS
    public async Task<ServiceResult<AdmissionClassResponse>>
        CreateClassAsync(CreateAdmissionClassRequest request)
    {
        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return ServiceResult<AdmissionClassResponse>.Failure(
                "Class name cannot be empty.", 400);
        }

        if (request.TotalSeats <= 0)
        {
            return ServiceResult<AdmissionClassResponse>.Failure(
                "Total seats must be greater than zero.", 400);
        }

        var admissionClass = new AdmissionClass
        {
            Name = name,
            TotalSeats = request.TotalSeats,
            CreatedAt = DateTime.UtcNow
        };

        _context.AdmissionClasses.Add(admissionClass);
        await _context.SaveChangesAsync();

        return ServiceResult<AdmissionClassResponse>.Success(
            MapToResponse(admissionClass, 0, 0),
            201);
    }

    // GET ALL CLASSES
    public async Task<ServiceResult<List<AdmissionClassResponse>>>
        GetAllClassesAsync()
    {
        var now = DateTime.UtcNow;

        var classes = await _context.AdmissionClasses
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();

        var seatCounts = await GetSeatCountsAsync(now);

        var responses = classes.Select(admissionClass =>
        {
            seatCounts.TryGetValue(
                admissionClass.Id,
                out var counts);

            return MapToResponse(
                admissionClass,
                counts.Confirmed,
                counts.Held);
        }).ToList();

        return ServiceResult<List<AdmissionClassResponse>>.Success(
            responses);
    }

    // GET CLASS BY ID
    public async Task<ServiceResult<AdmissionClassResponse>>
        GetClassByIdAsync(Guid classId)
    {
        var admissionClass = await _context.AdmissionClasses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == classId);

        if (admissionClass == null)
        {
            return ServiceResult<AdmissionClassResponse>.Failure(
                "Admission class not found.", 404);
        }

        var seatCounts = await GetSeatCountsAsync(
            DateTime.UtcNow,
            classId);

        seatCounts.TryGetValue(classId, out var counts);

        var response = MapToResponse(
            admissionClass,
            counts.Confirmed,
            counts.Held);

        return ServiceResult<AdmissionClassResponse>.Success(response);
    }

    public async Task<ServiceResult<bool>> DeleteClassAsync(Guid classId)
    {
        var admissionClass = await _context.AdmissionClasses
            .FirstOrDefaultAsync(c => c.Id == classId);

        if (admissionClass == null)
        {
            return ServiceResult<bool>.Failure(
                "Admission class not found.",
                404);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        await _context.AdmissionHolds
            .Where(h => h.ClassId == classId)
            .ExecuteDeleteAsync();

        _context.AdmissionClasses.Remove(admissionClass);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return ServiceResult<bool>.Success(true, 204);
    }
}