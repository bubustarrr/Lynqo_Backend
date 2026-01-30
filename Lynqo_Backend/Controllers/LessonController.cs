using Lynqo_Backend.Data;
using Lynqo_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Lynqo_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LessonsController : ControllerBase
    {
        private readonly LynqoDbContext _context;

        public LessonsController(LynqoDbContext context)
        {
            _context = context;
        }

        // GET: api/lessons/course/1
        // Get generic list of lessons (without units)
        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetLessonsByCourse(int courseId)
        {
            var lessons = await _context.Lessons
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.OrderIndex)
                .ToListAsync();

            return Ok(lessons);
        }

        // GET: api/lessons/course/1/structure
        // Get UNITS -> LESSONS hierarchy (The "Dashboard" view)
        [HttpGet("course/{courseId}/structure")]
        public async Task<IActionResult> GetCourseStructure(int courseId)
        {
            // Try to find the ID in "id", "sub", or the standard NameIdentifier claim
            var userIdClaim = User.FindFirst("id")
                           ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("sub");

            if (userIdClaim == null)
            {
                return Unauthorized("User ID claim missing. Token contents: " + string.Join(", ", User.Claims.Select(c => c.Type)));
            }

            int userId = int.Parse(userIdClaim.Value);

            var structure = await _context.Units
                .Where(u => u.CourseId == courseId)
                .OrderBy(u => u.OrderIndex)
                .Select(u => new
                {
                    u.Id,
                    u.Title,
                    u.Description,
                    Lessons = _context.Lessons
                        .Where(l => l.UnitId == u.Id)
                        .OrderBy(l => l.OrderIndex)
                        .Select(l => new
                        {
                            l.Id,
                            l.Title,
                            l.Type,
                            l.XpReward,
                            IsCompleted = _context.UserLessons.Any(ul => ul.UserId == userId && ul.LessonId == l.Id)
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(structure);
        }

        // GET: api/lessons/5
        // Get a specific lesson AND its contents (questions)
        // Used when the user clicks "Start Lesson"
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLesson(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null) return NotFound();

            var contents = await _context.LessonContents
                .Where(lc => lc.LessonId == id)
                .ToListAsync();

            // Map content to a cleaner structure with parsed JSON options
            var cleanedContents = contents.Select(c => new
            {
                c.Id,
                c.ContentType,
                c.Question,
                c.Answer,
                c.MediaId,
                // Try to parse options if they exist, otherwise null
                Options = string.IsNullOrEmpty(c.Options)
                    ? null
                    : JsonSerializer.Deserialize<object>(c.Options)
            });

            return Ok(new
            {
                Lesson = lesson,
                Contents = cleanedContents
            });
        }

        // POST: api/lessons
        // Create a new lesson (Admin only usually)
        [HttpPost]
        public async Task<ActionResult<Lesson>> CreateLesson(Lesson lesson)
        {
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLesson), new { id = lesson.Id }, lesson);
        }

        // DELETE: api/lessons/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null)
            {
                return NotFound();
            }

            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/lessons/5/complete
        [HttpPost("{id}/complete")]
        [Authorize] // Requires JWT Token
        public async Task<IActionResult> CompleteLesson(int id, [FromBody] LessonCompleteDto dto)
        {
            var userIdClaim = User.FindFirst("id")
               ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
               ?? User.FindFirst("sub");

            if (userIdClaim == null) return Unauthorized("User ID claim missing.");

            int userId = int.Parse(userIdClaim.Value);

            // 2. Check if lesson exists
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null) return NotFound("Lesson not found");

            // 3. Check if already completed (Optional: decide if you allow replays to give XP)
            var existingProgress = await _context.UserLessons
                .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LessonId == id);

            if (existingProgress != null)
            {
                // Update existing record (improve score)
                if (dto.Score > existingProgress.BestScore)
                {
                    existingProgress.BestScore = dto.Score;
                    existingProgress.Stars = Math.Max(existingProgress.Stars, dto.Stars);
                    // Note: We typically DO NOT give XP again for replays to prevent farming
                    await _context.SaveChangesAsync();
                }
                return Ok(new { Message = "Progress updated", NewTotalXp = -1 }); // -1 indicates no new XP
            }

            // 4. Create new UserLesson record
            var userLesson = new UserLesson
            {
                UserId = userId,
                LessonId = id,
                CompletedAt = DateTime.UtcNow,
                Stars = dto.Stars,
                XpEarned = dto.XpEarned,
                BestScore = dto.Score
            };
            _context.UserLessons.Add(userLesson);

            // 5. Award XP to User Profile
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                // Assuming your User model has 'Xp' column. 
                // Based on your SQL, you might need to check if 'xp' is in Users table or 'user_xp' table.
                // Your SQL had 'user_xp' table, but usually User table has a total cache too.
                // Let's assume you add it to the 'user_xp' table as well:

                var xpEntry = new UserXp
                {
                    UserId = userId,
                    XpAmount = dto.XpEarned,
                    Source = "lesson",
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserXp.Add(xpEntry);
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Lesson completed!", XpAwarded = dto.XpEarned });
        }

    }
}
