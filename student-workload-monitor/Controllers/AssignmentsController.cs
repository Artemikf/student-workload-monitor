using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using student_workload_monitor.Contracts;
using StudentWorkload.Application.Interfaces;
using StudentWorkload.Domain.Entities;

namespace student_workload_monitor.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AssignmentsController(IApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssignmentResponse>>> GetAll(
        [FromQuery] int? subjectId,
        [FromQuery] bool? isCompleted,
        CancellationToken ct)
    {
        var query = dbContext.Assignments.AsNoTracking();

        if (subjectId.HasValue)
        {
            query = query.Where(a => a.SubjectId == subjectId.Value);
        }

        if (isCompleted.HasValue)
        {
            query = query.Where(a => a.IsCompleted == isCompleted.Value);
        }

        var assignments = await query
            .OrderBy(a => a.DueDate)
            .Select(a => new AssignmentResponse(
                a.Id,
                a.SubjectId,
                a.Title,
                a.Description,
                a.DueDate,
                a.EstimatedHours,
                a.IsCompleted))
            .ToListAsync(ct);

        return Ok(assignments);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssignmentResponse>> GetById(int id, CancellationToken ct)
    {
        var assignment = await dbContext.Assignments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (assignment is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Assignment not found",
                Detail = $"Assignment with ID {id} does not exist."
            });
        }

        return Ok(new AssignmentResponse(
            assignment.Id,
            assignment.SubjectId,
            assignment.Title,
            assignment.Description,
            assignment.DueDate,
            assignment.EstimatedHours,
            assignment.IsCompleted));
    }

    [HttpPost]
    public async Task<ActionResult<AssignmentResponse>> Create([FromBody] CreateAssignmentRequest request, CancellationToken ct)
    {
        if (request.DueDate == default || request.DueDate.ToUniversalTime() < DateTime.UtcNow.AddMinutes(-5))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid deadline",
                Detail = "DueDate must be a valid future date."
            });
        }

        var subjectExists = await dbContext.Subjects.AnyAsync(s => s.Id == request.SubjectId, ct);
        if (!subjectExists)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid SubjectId",
                Detail = $"Subject with ID {request.SubjectId} does not exist."
            });
        }

        var assignment = new Assignment
        {
            SubjectId = request.SubjectId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            DueDate = request.DueDate.ToUniversalTime(),
            EstimatedHours = request.EstimatedHours,
            IsCompleted = false
        };

        dbContext.Assignments.Add(assignment);
        await dbContext.SaveChangesAsync(ct);

        var response = new AssignmentResponse(
            assignment.Id,
            assignment.SubjectId,
            assignment.Title,
            assignment.Description,
            assignment.DueDate,
            assignment.EstimatedHours,
            assignment.IsCompleted);

        return CreatedAtAction(nameof(GetById), new { id = assignment.Id }, response);
    }

    [HttpPatch("{id:int}/complete")]
    public async Task<IActionResult> MarkAsCompleted(int id, CancellationToken ct)
    {
        var assignment = await dbContext.Assignments.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (assignment is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Assignment not found",
                Detail = $"Assignment with ID {id} does not exist."
            });
        }

        assignment.IsCompleted = true;
        await dbContext.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var assignment = await dbContext.Assignments.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (assignment is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Assignment not found",
                Detail = $"Assignment with ID {id} does not exist."
            });
        }

        dbContext.Assignments.Remove(assignment);
        await dbContext.SaveChangesAsync(ct);

        return NoContent();
    }
}