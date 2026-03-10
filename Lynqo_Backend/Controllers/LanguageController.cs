// LanguagesController.cs
using Lynqo_Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LynqoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LanguagesController : ControllerBase
    {
        private readonly LynqoDbContext _context;

        public LanguagesController(LynqoDbContext context)
        {
            _context = context;
        }

        // GET: api/languages
        [HttpGet]
        public async Task<IActionResult> GetLanguages()
        {
            var langs = await _context.Languages
                .Select(l => new
                {
                    id = l.Id,
                    name = l.Name,
                    code = l.Code 
                })
                .ToListAsync();

            return Ok(langs);
        }
    }
}
