namespace Lynqo_Backend.Models.DTOs
{
    public class UserSettingsDTO
    {
        public bool DarkMode { get; set; }
        public bool SoundEnabled { get; set; }
        public int DailyGoalMinutes { get; set; }
        public string UiLanguage { get; set; }
        public bool NotificationsEnabled { get; set; }
    }

}
