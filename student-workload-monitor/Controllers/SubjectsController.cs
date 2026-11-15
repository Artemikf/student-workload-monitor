using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using student_workload_monitor.Contracts;
using StudentWorkload.Application.Interfaces;
using StudentWorkload.Domain.Entities;

namespace student_workload_monitor.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SubjectsController(IApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubjectResponse>>> GetAll([FromQuery] int? studentId, CancellationToken ct)
    {
        var query = dbContext.Subjects.AsNoTracking();

        if (studentId.HasValue)
        {
            query = query.Where(s => s.StudentId == studentId.Value);
        }

        var subjects = await query
            .Select(s => new SubjectResponse(s.Id, s.StudentId, s.Name, s.TeacherName, s.TotalCredits))
            .ToListAsync(ct);

        return Ok(subjects);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SubjectResponse>> GetById(int id, CancellationToken ct)
    {
        var subject = await dbContext.Subjects
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (subject is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Subject not found",
                Detail = $"Subject with ID {id} does not exist."
            });
        }

        return Ok(new SubjectResponse(subject.Id, subject.StudentId, subject.Name, subject.TeacherName, subject.TotalCredits));
    }

    [HttpPost]
    public async Task<ActionResult<SubjectResponse>> Create([FromBody] CreateSubjectRequest request, CancellationToken ct)
    {
        var studentExists = await dbContext.Students.AnyAsync(s => s.Id == request.StudentId, ct);
        if (!studentExists)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid StudentId",
                Detail = $"Student with ID {request.StudentId} does not exist."
            });
        }

        var subject = new Subject
        {
            StudentId = request.StudentId,
            Name = request.Name.Trim(),
            TeacherName = request.TeacherName.Trim(),
            TotalCredits = request.TotalCredits
        };

        dbContext.Subjects.Add(subject);
        await dbContext.SaveChangesAsync(ct);

        var response = new SubjectResponse(subject.Id, subject.StudentId, subject.Name, subject.TeacherName, subject.TotalCredits);
        return CreatedAtAction(nameof(GetById), new { id = subject.Id }, response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var subject = await dbContext.Subjects.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (subject is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Subject not found",
                Detail = $"Subject with ID {id} does not exist."
            });
        }

        dbContext.Subjects.Remove(subject);
        await dbContext.SaveChangesAsync(ct);

        return NoContent();
    }
}