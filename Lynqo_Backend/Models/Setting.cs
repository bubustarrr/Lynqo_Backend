namespace Lynqo_Backend.Models
{
    public class Setting
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public bool DarkMode { get; set; } = false;
        public bool SoundEnabled { get; set; } = true;
        public int DailyGoalMinutes { get; set; } = 15;
        public string UiLanguage { get; set; } = "en";
        public bool NotificationsEnabled { get; set; } = true;

        public User User { get; set; } // Navigation property
    }

}
