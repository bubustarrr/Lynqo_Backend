using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lynqo_Backend.Models
{
    [Table("settings")]
    public class Setting
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("dark_mode")]
        public bool DarkMode { get; set; }

        [Column("sound_enabled")]
        public bool SoundEnabled { get; set; }


        [Column("ui_language")]
        public string UiLanguage { get; set; }

        [Column("notifications_enabled")]
        public bool NotificationsEnabled { get; set; }

        public User? User { get; set; }
    }
}
