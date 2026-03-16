using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lynqo_Backend.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("username")]
        public string Username { get; set; } = null!;

        [Column("display_name")]
        public string? DisplayName { get; set; }

        [Column("email")]
        public string Email { get; set; } = null!;

        [Column("password_hash")]
        public string? PasswordHash { get; set; }

        [Column("profile_pic_url")]
        public string? ProfilePicUrl { get; set; }

        [Column("hearts")]
        public int Hearts { get; set; } = 5;

        [Column("coins")]
        public int Coins { get; set; } = 0;

        [Column("is_premium")]
        public bool IsPremium { get; set; } = false;

        [Column("role")]
        public string Role { get; set; } = "user";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("last_heart_refill_at")]
        public DateTime? LastHeartRefillAt { get; set; }

        [Column("is_verified")]
        public bool IsVerified { get; set; } = false;

        [Column("verification_token")]
        public string? VerificationToken { get; set; }

        // --- ÚJ MEZŐK: ELFELEJTETT JELSZÓ ---
        [Column("reset_token")]
        public string? ResetToken { get; set; }

        [Column("reset_token_expiry")]
        public DateTime? ResetTokenExpiry { get; set; }

        // --- ÚJ MEZŐK A STREAK-HEZ ---
        [Column("streak")]
        public int Streak { get; set; } = 0;

        [Column("last_lesson_date")]
        public DateTime? LastLessonDate { get; set; }

        [Column("league")]
        public string League { get; set; } = "Bronze";
        // Navigation properties (needed by DbContext)
        public ICollection<UserXp> XpHistory { get; set; } = new List<UserXp>();
        public ICollection<UserLesson> UserLessons { get; set; } = new List<UserLesson>();
        public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
    }
}