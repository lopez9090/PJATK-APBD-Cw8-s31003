using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PJATK_APBD_Cw8_s31003.Models;
using PJATK_APBD_Cw8_s31003.DTOs;

namespace PJATK_APBD_Cw8_s31003.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly HospitalDbContext _context;

    public PatientsController(HospitalDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetPatients([FromQuery] string? search)
    {
        var query = _context.Patients
            .Include(p => p.Admissions)
                .ThenInclude(a => a.Ward)
            .Include(p => p.BedAssignments)
                .ThenInclude(ba => ba.Bed)
                    .ThenInclude(b => b.BedType)
            .Include(p => p.BedAssignments)
                .ThenInclude(ba => ba.Bed)
                    .ThenInclude(b => b.Room)
                        .ThenInclude(r => r.Ward)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => 
                EF.Functions.Like(p.FirstName, $"%{search}%") || 
                EF.Functions.Like(p.LastName, $"%{search}%"));
        }

        var patients = await query.Select(p => new PatientGetDto
        {
            Pesel = p.Pesel,
            FirstName = p.FirstName,
            LastName = p.LastName,
            Age = p.Age,
            Sex = p.Sex ? "Male" : "Female", 
            Admissions = p.Admissions.Select(a => new AdmissionDto
            {
                Id = a.Id,
                AdmissionDate = a.AdmissionDate,
                DischargeDate = a.DischargeDate,
                Ward = new WardDto
                {
                    Id = a.Ward.Id,
                    Name = a.Ward.Name,
                    Description = a.Ward.Description
                }
            }).ToList(),
            BedAssignments = p.BedAssignments.Select(ba => new BedAssignmentDto
            {
                Id = ba.Id,
                From = ba.From,
                To = ba.To,
                Bed = new BedDto
                {
                    Id = ba.Bed.Id,
                    BedType = new BedTypeDto
                    {
                        Id = ba.Bed.BedType.Id,
                        Name = ba.Bed.BedType.Name,
                        Description = ba.Bed.BedType.Description
                    },
                    Room = new RoomDto
                    {
                        Id = ba.Bed.Room.Id,
                        HasTv = ba.Bed.Room.HasTv,
                        Ward = new WardDto
                        {
                            Id = ba.Bed.Room.Ward.Id,
                            Name = ba.Bed.Room.Ward.Name,
                            Description = ba.Bed.Room.Ward.Description
                        }
                    }
                }
            }).ToList()
        }).ToListAsync();

        return Ok(patients);
    }
    
    [HttpPost("{id}/bedassignments")]
    public async Task<IActionResult> AddBedAssignment(string id, [FromBody] BedAssignmentPostDto request)
    {
        var patientExists = await _context.Patients.AnyAsync(p => p.Pesel == id);
        if (!patientExists)
        {
            return NotFound($"Pacjent o numererze PESEL {id} nie został znaleziony w bazie.");
        }

        var availableBed = await _context.Beds
            .Include(b => b.BedType)
            .Include(b => b.Room)
            .ThenInclude(r => r.Ward)
            .Where(b => b.BedType.Name == request.BedType && b.Room.Ward.Name == request.Ward)
            .Where(b => !b.BedAssignments.Any(ba =>
                (request.To == null || ba.From < request.To) &&
                (ba.To == null || ba.To > request.From)
            ))
            .FirstOrDefaultAsync();

        if (availableBed == null)
        {
            return NotFound($"Brak wolnych łóżek typu '{request.BedType}' na oddziale '{request.Ward}' w podanym okresie czasu.");
        }

        var newAssignment = new BedAssignment
        {
            PatientPesel = id,
            BedId = availableBed.Id,
            From = request.From,
            To = request.To
        };

        _context.BedAssignments.Add(newAssignment);
        await _context.SaveChangesAsync();

        return StatusCode(201, "Łóżko zostało poprawnie przypisane pacjentowi.");
    }
}