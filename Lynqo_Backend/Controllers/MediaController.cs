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
    }
}
