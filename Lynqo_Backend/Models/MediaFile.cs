using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lynqo_Backend.Models
{
    [Table("media_files")]
    public class MediaFile
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("file_url")]
        public string FileUrl { get; set; }

        [Column("file_type")]
        public string FileType { get; set; }

        [Column("language")]
        public string? Language { get; set; }

        [Column("uploader_id")]
        public int? UploaderId { get; set; }

        [Column("used_in")]
        public string? UsedIn { get; set; }

        [Column("uploaded_at")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
