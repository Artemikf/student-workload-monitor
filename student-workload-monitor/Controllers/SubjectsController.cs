using Microsoft.AspNetCore.Mvc;
using StudentWorkload.Application.Interfaces;
using StudentWorkload.Domain.Entities;

namespace student_workload_monitor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubjectsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public SubjectsController(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Создать новый учебный предмет
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSubject([FromBody] Subject subject)
    {
        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(CreateSubject), new { id = subject.Id }, subject);
    }
}