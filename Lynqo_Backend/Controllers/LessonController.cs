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
            var userIdClaim = User.FindFirst("id")
               ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
               ?? User.FindFirst("sub");

            if (userIdClaim == null) return Unauthorized("User ID claim missing.");

            int userId = int.Parse(userIdClaim.Value);

            // 1. Check if lesson exists
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null) return NotFound("Lesson not found");

            // 2. Retrieve User to update Hearts only
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            // Update Hearts
            user.Hearts = dto.HeartsRemaining;

            // 3. Add XP Entry to History Table (Global History)
            var xpEntry = new UserXp
            {
                UserId = userId,
                XpAmount = dto.XpEarned,
                Source = "lesson",
                CreatedAt = DateTime.UtcNow
            };
            _context.UserXp.Add(xpEntry);

            // --- NEW: Update Course-Specific XP (UserCourses Table) ---
            // Find the record for this user + this course (e.g., English->Spanish)
            // Ensure your Lesson model has 'CourseId' or navigate via Unit->Course
            // If Lesson doesn't have CourseId directly, retrieve it via Unit
            int courseId = lesson.CourseId;
            if (courseId == 0) // Fallback if property isn't populated directly
            {
                var unit = await _context.Units.FindAsync(lesson.UnitId);
                if (unit != null) courseId = unit.CourseId;
            }

            var userCourse = await _context.UserCourses
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CourseId == courseId);

            if (userCourse == null)
            {
                // First time playing this course? Create the record.
                userCourse = new UserCourse
                {
                    UserId = userId,
                    CourseId = courseId,
                    TotalXp = 0,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserCourses.Add(userCourse);
            }

            // Add XP to this specific language course
            userCourse.TotalXp += dto.XpEarned;
            // -----------------------------------------------------------

            // 4. Update or Create Lesson Progress (Stars/Completion)
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

            // Save everything (User updates, XP history, UserCourse XP, Lesson Progress)
            await _context.SaveChangesAsync();

            // 5. Response
            return Ok(new
            {
                Message = "Lesson completed!",
                XpAwarded = dto.XpEarned,
                NewCourseXp = userCourse.TotalXp, // Return the specific language XP
                Hearts = user.Hearts
            });
        }
    }
}