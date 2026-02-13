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
        [Authorize]
        public async Task<IActionResult> CompleteLesson(int id, [FromBody] LessonCompleteDto dto)
        {
            var userIdClaim = User.FindFirst("id") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);

            // 1. Update Hearts on User
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");
            user.Hearts = dto.HeartsRemaining;

            // 2. Insert XP History Record (This is how we track XP now)
            var xpEntry = new UserXp
            {
                UserId = userId,
                XpAmount = dto.XpEarned,
                Source = "lesson",
                CreatedAt = DateTime.UtcNow
            };

            // 🔥 IMPORTANT: Ensure this matches your DbContext property name!
            // It is likely _context.UserXp or _context.UserXps. Check your DbContext!
            _context.UserXps.Add(xpEntry);

            // 3. Update Lesson Progress
            var existingProgress = await _context.UserLessons
                .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LessonId == id);

            if (existingProgress != null)
            {
                if (dto.Score > existingProgress.BestScore)
                {
                    existingProgress.BestScore = dto.Score;
                    existingProgress.Stars = Math.Max(existingProgress.Stars, dto.Stars);
                }
            }
            else
            {
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
            }

            await _context.SaveChangesAsync();

            // 4. Calculate New Total XP to return to frontend
            // We sum up all records in the UserXp table for this user
            int currentTotalXp = await _context.UserXps
                .Where(x => x.UserId == userId)
                .SumAsync(x => x.XpAmount);

            return Ok(new
            {
                Message = "Lesson completed!",
                XpAwarded = dto.XpEarned,
                Hearts = user.Hearts,
                TotalXp = currentTotalXp // Sending the calculated sum back
            });
        }

    }
}