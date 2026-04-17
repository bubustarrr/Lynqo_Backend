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

            var uniqueFileName = $"{Guid.NewGuid()}_{uploadDto.File.FileName}";

            // Include language subfolder if provided
            var folderName = string.IsNullOrEmpty(uploadDto.Language)
                ? Path.Combine("media", uploadDto.FileType)
                : Path.Combine("media", uploadDto.FileType, uploadDto.Language);  // ← e.g. media/audio/french

            var uploadPath = Path.Combine(_environment.WebRootPath, folderName);
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var fullPath = Path.Combine(uploadPath, uniqueFileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
                await uploadDto.File.CopyToAsync(stream);

            var relativePath = $"/{folderName.Replace("\\", "/")}/{uniqueFileName}";

            var mediaFile = new MediaFile
            {
                FileUrl = relativePath,
                FileType = uploadDto.FileType,
                Language = uploadDto.Language,
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
        [HttpGet("audio/{language}/{fileName}")]
        public IActionResult GetAudioFile(string language, string fileName)
        {
            try
            {
                var rootPath = _environment.WebRootPath;
                if (string.IsNullOrEmpty(rootPath))
                    rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                // Sanitize inputs to prevent path traversal attacks
                language = Path.GetFileName(language);
                fileName = Path.GetFileName(fileName);

                var filePath = Path.Combine(rootPath, "media", "audio", language, fileName);

                Console.WriteLine($"[AUDIO] Requested: {language}/{fileName}");
                Console.WriteLine($"[AUDIO] Full path: {filePath}");
                Console.WriteLine($"[AUDIO] Exists: {System.IO.File.Exists(filePath)}");

                if (!System.IO.File.Exists(filePath))
                    return NotFound($"Audio not found: {language}/{fileName}");

                var ext = Path.GetExtension(filePath).ToLower();
                var contentType = ext switch
                {
                    ".mp3" => "audio/mpeg",
                    ".ogg" => "audio/ogg",
                    ".wav" => "audio/wav",
                    _ => "application/octet-stream"
                };

                return PhysicalFile(filePath, contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetMediaById(int id)
        {
            // 1. NAPLÓZÁS: Látni fogjuk, ha egyáltalán bejött a kérés
            Console.WriteLine($"\n[AUDIO DEBUG] --- ÚJ KÉRÉS ÉRKEZETT AZ ID-RE: {id} ---");

            try
            {
                var mediaFile = await _context.MediaFiles.FindAsync(id);
                if (mediaFile == null)
                {
                    Console.WriteLine($"[AUDIO DEBUG] ❌ HIBA: Nincs {id}-es ID a media_files táblában!");
                    return NotFound("Media nem található az adatbázisban.");
                }

                Console.WriteLine($"[AUDIO DEBUG] ✅ Adatbázisban megtalálva. Fájl URL: {mediaFile.FileUrl}");

                var rootPath = _environment.WebRootPath;
                if (string.IsNullOrEmpty(rootPath))
                {
                    rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                }

                // Biztonságos útvonal összefűzés
                var relativePath = mediaFile.FileUrl?.TrimStart('/', '\\') ?? "";
                var filePath = Path.Combine(rootPath, relativePath);

                Console.WriteLine($"[AUDIO DEBUG] Keresett fizikai mappa útvonal: {filePath}");

                if (!System.IO.File.Exists(filePath))
                {
                    Console.WriteLine($"[AUDIO DEBUG] ❌ HIBA: A fájl fizikailag nem létezik ezen a helyen!");
                    return NotFound($"A fájl fizikailag nem található: {filePath}");
                }

                var ext = Path.GetExtension(filePath).ToLower();
                var contentType = ext == ".mp3" ? "audio/mpeg" : (ext == ".wav" ? "audio/wav" : "application/octet-stream");

                Console.WriteLine($"[AUDIO DEBUG] ✅ Fájl sikeresen kiküldve a böngészőnek! Típus: {contentType}");

                // A 'true' a végén nagyon fontos! Ez engedi a böngészőnek a hang lejátszását és tekerését.
                return PhysicalFile(filePath, contentType, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDIO DEBUG] ❌ VÉGZETES BELSŐ HIBA: {ex.Message}");
                return StatusCode(500, "Belső szerverhiba történt: " + ex.Message);
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
