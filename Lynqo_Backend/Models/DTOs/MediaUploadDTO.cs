using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Lynqo_Backend.Models
{
    public class MediaUploadDto
    {
        [Required]
        public IFormFile File { get; set; }

        [Required]
        public string FileType { get; set; } = "image"; // "image", "audio", "video"
    }
}
