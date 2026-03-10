using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LynqoBackend.Models
{
    [Table("quests")]
    public class Quest
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("rewardxp")]
        public int RewardXp { get; set; }

        [Column("duration")]
        public string Duration { get; set; } = "daily"; // daily | weekly

        [Column("type")]
        public string Type { get; set; } = "lesson"; // lesson | practice

        [NotMapped]
        public int TargetAmount { get; set; } = 3;
    }
}
