using System.ComponentModel.DataAnnotations.Schema;

namespace Lynqo_Backend.Models
{
    public class Badge
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        [Column("icon_url")]
        public string? IconUrl { get; set; }
        public string Type { get; set; } = "milestone"; // milestone, monthly
    }
}
