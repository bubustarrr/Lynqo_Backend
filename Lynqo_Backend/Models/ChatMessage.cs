using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lynqo_Backend.Models
{
    [Table("chat_messages")]
    public class ChatMessage
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("sender_id")]
        public int SenderId { get; set; }

        [Column("receiver_id")]
        public int ReceiverId { get; set; }

        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [Column("timestamp")]
        public DateTime Timestamp { get; set; }

        [Column("read_at")]
        public DateTime? ReadAt { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        [Column("is_reported")]
        public bool IsReported { get; set; }

        public User? Sender { get; set; }
        public User? Receiver { get; set; }

    }
}
