using Lynqo_Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Lynqo_Backend.Models.Services;

public class AiService
{
    private readonly LynqoDbContext _context;
    public AiService(LynqoDbContext context) => _context = context;

    public async Task<string> GenerateReplyAsync(int? lessonId, string userMessage)
    {
        // Placeholder: later replace with OpenAI/Anthropic/etc.
        if (lessonId == null) return "Tell me what you want to practice (vocab, listening, or speaking).";

        var first = await _context.LessonContents
            .Where(lc => lc.LessonId == lessonId)
            .Select(lc => new { lc.Question, lc.Answer })
            .FirstOrDefaultAsync();

        if (first == null) return "Let’s start with a simple warm-up. Translate a short phrase you know.";

        return $"Practice: {first.Question} (Answer: {first.Answer}). Now reply with your own example sentence.";
    }
}
