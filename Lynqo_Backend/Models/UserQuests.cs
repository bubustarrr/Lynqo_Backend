using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LynqoBackend.Models;

namespace Lynqo_Backend.Models
{
    [Table("user_quests")]
    public class UserQuest
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("quest_id")]
        public int QuestId { get; set; }

        [Column("progress")]
        public int Progress { get; set; } = 0;

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        public Quest? Quest { get; set; }
    }
}
