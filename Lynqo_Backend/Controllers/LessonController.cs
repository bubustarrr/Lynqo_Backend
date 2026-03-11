using Lynqo_Backend.Data;
using Lynqo_Backend.Models;
using Lynqo_Backend.Models.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Lynqo_Backend.Controllers
{
    // -------------------------------------------------------------
    // DTOs for the LessonsController
    // -------------------------------------------------------------
    public class SyncHeartsDto
    {
        public int HeartsRemaining { get; set; }
    }

    public class LessonCompleteDto
    {
        public int Score { get; set; }
        public int Stars { get; set; }
        public int XpEarned { get; set; }
        public int HeartsRemaining { get; set; }
        public int TimeSpentSeconds { get; set; }
    }

    // -------------------------------------------------------------
    // Controller 
    // -------------------------------------------------------------
    [Route("api/[controller]")]
    [ApiController]
    public class LessonsController : ControllerBase
    {
        private readonly LynqoDbContext _context;
        private readonly GamificationService _gamificationService;

        public LessonsController(LynqoDbContext context, GamificationService gamificationService)
        {
            _context = context;
            _gamificationService = gamificationService;
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetLessonsByCourse(int courseId)
        {
            var lessons = await _context.Lessons
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.OrderIndex)
                .ToListAsync();

            return Ok(lessons);
        }

        [HttpGet("course/{courseId}/structure")]
        public async Task<IActionResult> GetCourseStructure(int courseId)
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var units = await _context.Units
                .Where(u => u.CourseId == courseId)
                .OrderBy(u => u.OrderIndex)
                .Select(u => new
                {
                    u.Id,
                    u.Title,
                    u.Description,
                    Lessons = u.Lessons
                        .Where(l => l.CourseId == courseId)
                        .OrderBy(l => l.OrderIndex)
                        .Select(l => new
                        {
                            l.Id,
                            l.Title,
                            l.Type,
                            IsCompleted = _context.UserLessons.Any(ul => ul.UserId == userId && ul.LessonId == l.Id)
                        }).ToList()
                })
                .ToListAsync();

            return Ok(units);
        }

        // GET: api/lessons/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetLesson(int id)
        {
            // Check who is requesting the lesson
            var userIdClaim = User.FindFirst("id") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int userId = int.Parse(userIdClaim.Value);

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            // Block entry if they have 0 hearts and are not Premium!
            if (user.Hearts <= 0 && !user.IsPremium)
            {
                return BadRequest(new { Message = "NO_HEARTS" });
            }

            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null) return NotFound();

            var contents = await _context.LessonContents
                .Where(lc => lc.LessonId == id)
                .ToListAsync();

            var cleanedContents = contents.Select(c => new
            {
                c.Id,
                c.ContentType,
                c.Question,
                c.Answer,
                c.MediaId,
                Options = string.IsNullOrEmpty(c.Options)
                    ? null
                    : JsonSerializer.Deserialize<object>(c.Options)
            });

            return Ok(new
            {
                Lesson = lesson,
                Contents = cleanedContents,
                Hearts = user.Hearts,
                IsPremium = user.IsPremium
            });
        }

        [HttpPost]
        public async Task<ActionResult<Lesson>> CreateLesson(Lesson lesson)
        {
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLesson), new { id = lesson.Id }, lesson);
        }

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

            // 2. Insert XP History Record 
            var xpEntry = new UserXp
            {
                UserId = userId,
                XpAmount = dto.XpEarned,
                Source = "lesson",
                CreatedAt = DateTime.UtcNow
            };
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

            // 4. TRIGGERS FOR DAILY QUESTS
            await _gamificationService.UpdateQuestProgressAsync(userId, 1, 1);
            await _gamificationService.UpdateQuestProgressAsync(userId, 2, 1);

            // 5. Calculate New Total XP to return to frontend
            int currentTotalXp = await _context.UserXps
                .Where(x => x.UserId == userId)
                .SumAsync(x => x.XpAmount);

            return Ok(new
            {
                Message = "Lesson completed!",
                XpAwarded = dto.XpEarned,
                Hearts = user.Hearts,
                TotalXp = currentTotalXp
            });
        }

        // NEW ENDPOINT: Save hearts if a user dies or quits early
        [HttpPost("sync-hearts")]
        [Authorize]
        public async Task<IActionResult> SyncHearts([FromBody] SyncHeartsDto dto)
        {
            var userIdClaim = User.FindFirst("id") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            // Don't deduct hearts if they are premium
            if (!user.IsPremium)
            {
                user.Hearts = Math.Max(0, dto.HeartsRemaining);
                await _context.SaveChangesAsync();
            }

            return Ok(new { Hearts = user.Hearts });
        }
    }
}
