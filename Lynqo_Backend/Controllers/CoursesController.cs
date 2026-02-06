using Lynqo_Backend.Data;
using Lynqo_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lynqo_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly LynqoDbContext _context;

        public CoursesController(LynqoDbContext context)
        {
            _context = context;
        }

        // GET: api/Courses
        // Optional: Filter by source language ID (e.g., ?sourceId=1)
        [HttpGet]
        public async Task<IActionResult> GetCourses([FromQuery] int? sourceId)
        {
            var query = _context.Courses.AsQueryable();

            // Filter by source language if provided
            if (sourceId.HasValue)
            {
                query = query.Where(c => c.SourceLanguageId == sourceId.Value);
            }

            // Always filter for active courses only
            query = query.Where(c => c.IsActive);

            var courses = await query.ToListAsync();
            return Ok(courses);
        }

        // GET: api/Courses/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();
            return Ok(course);
        }
    }
}
