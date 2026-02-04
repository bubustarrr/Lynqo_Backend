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
        [HttpGet("media/audio/french/{number:int}")]
        public IActionResult GetFrenchNumberDirect(int number)
        {
            var filename = GetFrenchNumberFilename(number);
            var filePath = Path.Combine(_environment.WebRootPath, "media/audio/french", filename);

            if (!System.IO.File.Exists(filePath))
                return NotFound($"Audio not found: {filename}");

            return PhysicalFile(filePath, "audio/ogg");
        }


        // ADD THIS HELPER METHOD AT THE BOTTOM (inside the class)
        private string GetFrenchNumberFilename(int number) => number switch
        {
            0 => "zero.ogg",
            1 => "un.ogg",
            2 => "deux.ogg",
            3 => "trois.ogg",
            4 => "quatre.ogg",
            5 => "cinq.ogg",
            6 => "six.ogg",
            7 => "sept.ogg",
            8 => "huit.ogg",
            9 => "neuf.ogg",
            10 => "dix.ogg",
            11 => "onze.ogg",
            12 => "douze.ogg",
            13 => "treize.ogg",
            14 => "quatorze.ogg",
            15 => "quinze.ogg",
            16 => "seize.ogg",
            17 => "dix-sept.ogg",
            18 => "dix-huit.ogg",
            19 => "dix-neuf.ogg",
            20 => "vingt.ogg",
            30 => "trente.ogg",
            40 => "quarante.ogg",
            50 => "cinquante.ogg",
            60 => "soixante.ogg",
            70 => "soixante-dix.ogg",
            80 => "quatre-vingts.ogg",
            90 => "quatre-vingt-dix.ogg",
            100 => "cent.ogg",
            _ => throw new ArgumentException("Number not supported")
        };
    }
}
