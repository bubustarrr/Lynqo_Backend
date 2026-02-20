using Lynqo_Backend.Data;
using Lynqo_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lynqo_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly LynqoDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public MediaController(LynqoDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] MediaUploadDto uploadDto)
        {
            if (uploadDto.File == null || uploadDto.File.Length == 0)
                return BadRequest("No file uploaded.");

            // 1. Create unique filename
            var uniqueFileName = $"{Guid.NewGuid()}_{uploadDto.File.FileName}";

            // 2. Determine folder based on type (audio/image/video)
            // e.g., wwwroot/media/audio
            var folderName = Path.Combine("media", uploadDto.FileType);
            var uploadPath = Path.Combine(_environment.WebRootPath, folderName);

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var fullPath = Path.Combine(uploadPath, uniqueFileName);

            // 3. Save file to disk
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await uploadDto.File.CopyToAsync(stream);
            }

            // 4. Save metadata to Database
            var relativePath = $"/{folderName.Replace("\\", "/")}/{uniqueFileName}"; // For URL usage

            var mediaFile = new MediaFile
            {
                FileUrl = relativePath,
                FileType = uploadDto.FileType,
                UploadedAt = DateTime.UtcNow
            };


            _context.MediaFiles.Add(mediaFile);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Upload successful", FileUrl = relativePath, Id = mediaFile.Id });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var files = await _context.MediaFiles.ToListAsync();
            return Ok(files);
        }
        [HttpGet("audio/french/{number:int}")]
        public IActionResult GetFrenchNumberDirect(int number)
        {
            try
            {
                var filename = GetFrenchNumberFilename(number);

                // Safe way to get wwwroot path
                var rootPath = _environment.WebRootPath;
                if (string.IsNullOrEmpty(rootPath))
                {
                    rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                }

                var filePath = Path.Combine(rootPath, "media", "audio", "french", filename);

                // DEBUG LOGGING
                Console.WriteLine($"[AUDIO DEBUG] Request for number: {number}");
                Console.WriteLine($"[AUDIO DEBUG] Looking at path: {filePath}");
                Console.WriteLine($"[AUDIO DEBUG] File Exists: {System.IO.File.Exists(filePath)}");

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound($"Audio file missing on server at: {filePath}");
                }

                // Add CORS headers so React can play it
                Response.Headers.Add("Access-Control-Allow-Origin", "*");
                Response.Headers.Add("Access-Control-Allow-Methods", "GET");

                // Return the file
                return PhysicalFile(filePath, "audio/mpeg");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }




        // ADD THIS HELPER METHOD AT THE BOTTOM (inside the class)
        private string GetFrenchNumberFilename(int number) => number switch
        {
            0 => "zero.mp3",
            1 => "un.mp3",
            2 => "deux.mp3",
            3 => "trois.mp3",
            4 => "quatre.mp3",
            5 => "cinq.mp3",
            6 => "six.mp3",
            7 => "sept.mp3",
            8 => "huit.mp3",
            9 => "neuf.mp3",
            10 => "dix.mp3",
            11 => "onze.mp3",
            12 => "douze.mp3",
            13 => "treize.mp3",
            14 => "quatorze.mp3",
            15 => "quinze.mp3",
            16 => "seize.mp3",
            17 => "dix-sept.mp3",
            18 => "dix-huit.mp3",
            19 => "dix-neuf.mp3",
            20 => "vingt.mp3",
            30 => "trente.mp3",
            40 => "quarante.mp3",
            50 => "cinquante.mp3",
            60 => "soixante.mp3",
            70 => "soixante-dix.mp3",
            80 => "quatre-vingts.mp3",
            90 => "quatre-vingt-dix.mp3",
            100 => "cent.mp3",
            _ => throw new ArgumentException("Number not supported")
        };
    }
}
