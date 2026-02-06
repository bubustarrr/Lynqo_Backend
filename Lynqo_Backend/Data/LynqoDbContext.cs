using Microsoft.EntityFrameworkCore;
using Lynqo_Backend.Models;
using LynqoBackend.Models;
namespace Lynqo_Backend.Data;

public class LynqoDbContext : DbContext
{
    public LynqoDbContext(DbContextOptions<LynqoDbContext> options) : base(options) { }
    public DbSet<User> Users { get; set; }
    public DbSet<Setting> Settings { get; set; }
    public DbSet<UserXp> UserXps { get; set; }
    public DbSet<Leaderboard> Leaderboards { get; set; }
    public DbSet<LeaderboardEntry> LeaderboardEntries { get; set; }
    public DbSet<Badge> Badges { get; set; }
    public DbSet<UserBadge> UserBadges { get; set; }
    public DbSet<MediaFile> MediaFiles { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<UserLesson> UserLessons { get; set; }
    public DbSet<LessonContent> LessonContents { get; set; }
    public DbSet<UserXp> UserXp { get; set; }
    public DbSet<Friendship> Friendships { get; set; }
    public DbSet<StoreItem> StoreItems { get; set; }
    public DbSet<UserPurchase> UserPurchases { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Quest> Quests { get; set; }
    public DbSet<UserQuest> UserQuests { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Analytics> Analytics { get; set; }
    public DbSet<PracticeSession> PracticeSessions { get; set; }
    public DbSet<AiSession> AiSessions { get; set; }
    public DbSet<AiMessage> AiMessages { get; set; }
    public DbSet<BannedUser> BannedUsers { get; set; }
    public DbSet<AdminLog> AdminLogs { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<ApiToken> ApiTokens { get; set; }
    public DbSet<Course> Courses { get; set; }




    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Table name mappings (SQL uses plural snake_case)
        modelBuilder.Entity<UserXp>().ToTable("user_xp");
        modelBuilder.Entity<Leaderboard>().ToTable("leaderboards");
        modelBuilder.Entity<LeaderboardEntry>().ToTable("leaderboard_entries");
        modelBuilder.Entity<Badge>().ToTable("badges");
        modelBuilder.Entity<UserBadge>().ToTable("user_badges");
        modelBuilder.Entity<Friendship>().ToTable("friendships");
        modelBuilder.Entity<StoreItem>().ToTable("store_items");
        modelBuilder.Entity<UserPurchase>().ToTable("user_purchases");
        modelBuilder.Entity<Subscription>().ToTable("subscriptions");
        modelBuilder.Entity<Quest>().ToTable("quests");
        modelBuilder.Entity<UserQuest>().ToTable("user_quests");
        modelBuilder.Entity<ChatMessage>().ToTable("chat_messages");
        modelBuilder.Entity<Report>().ToTable("reports");
        modelBuilder.Entity<Notification>().ToTable("notifications");
        modelBuilder.Entity<Analytics>().ToTable("analytics");
        modelBuilder.Entity<PracticeSession>().ToTable("practice_sessions");
        modelBuilder.Entity<AiSession>().ToTable("ai_sessions");
        modelBuilder.Entity<AiMessage>().ToTable("ai_messages");
        modelBuilder.Entity<BannedUser>().ToTable("banned_users");
        modelBuilder.Entity<AdminLog>().ToTable("admin_logs");
        modelBuilder.Entity<AuditLog>().ToTable("audit_logs");




        modelBuilder.Entity<User>()
            .Property(u => u.CreatedAt)
            .HasColumnName("created_at");
        modelBuilder.Entity<UserXp>()
            .HasOne(ux => ux.User)
            .WithMany(u => u.XpHistory)
            .HasForeignKey(ux => ux.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LeaderboardEntry>()
            .HasOne(le => le.Leaderboard)
            .WithMany()
            .HasForeignKey(le => le.LeaderboardId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LeaderboardEntry>()
            .HasOne(le => le.User)
            .WithMany()
            .HasForeignKey(le => le.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserBadge>()
            .HasOne(ub => ub.User)
            .WithMany()
            .HasForeignKey(ub => ub.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserBadge>()
            .HasOne(ub => ub.Badge)
            .WithMany()
            .HasForeignKey(ub => ub.BadgeId)
            .OnDelete(DeleteBehavior.Cascade);
        // --- BannedUser Configuration (Fixes your error) ---
        modelBuilder.Entity<BannedUser>()
            .HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade); // If user is deleted, ban record is deleted

        modelBuilder.Entity<BannedUser>()
            .HasOne(b => b.Issuer)
            .WithMany()
            .HasForeignKey(b => b.IssuedBy)
            .OnDelete(DeleteBehavior.SetNull); // If admin is deleted, keep ban record but clear issuer

        // --- AdminLog Configuration (Similar issue potential) ---
        modelBuilder.Entity<AdminLog>()
            .HasOne(al => al.Admin)
            .WithMany()
            .HasForeignKey(al => al.AdminId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AdminLog>()
            .HasOne(al => al.TargetUser)
            .WithMany()
            .HasForeignKey(al => al.TargetUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // --- Friendship Configuration ---
        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.Sender)
            .WithMany()
            .HasForeignKey(f => f.SenderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.Receiver)
            .WithMany()
            .HasForeignKey(f => f.ReceiverId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- ChatMessage Configuration ---
        modelBuilder.Entity<ChatMessage>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChatMessage>()
            .HasOne(m => m.Receiver)
            .WithMany()
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- Report Configuration ---
        modelBuilder.Entity<Report>()
            .HasOne(r => r.Reporter)
            .WithMany()
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Report>()
            .HasOne(r => r.Resolver)
            .WithMany()
            .HasForeignKey(r => r.ResolvedBy)
            .OnDelete(DeleteBehavior.SetNull);
        // --- 1. QUESTS Mappings ---
        modelBuilder.Entity<Quest>()
            .Property(q => q.RewardXp).HasColumnName("reward_xp");

        modelBuilder.Entity<UserQuest>()
            .Property(uq => uq.UserId).HasColumnName("user_id");
        modelBuilder.Entity<UserQuest>()
            .Property(uq => uq.QuestId).HasColumnName("quest_id");
        modelBuilder.Entity<UserQuest>()
            .Property(uq => uq.CompletedAt).HasColumnName("completed_at");

        // --- 2. STORE Mappings ---
        modelBuilder.Entity<StoreItem>()
            .Property(s => s.MaxQuantity).HasColumnName("max_quantity");

        modelBuilder.Entity<UserPurchase>()
            .Property(up => up.UserId).HasColumnName("user_id");
        modelBuilder.Entity<UserPurchase>()
            .Property(up => up.ItemId).HasColumnName("item_id");
        modelBuilder.Entity<UserPurchase>()
            .Property(up => up.PurchasedAt).HasColumnName("purchased_at");

        // --- 3. SUBSCRIPTIONS Mappings ---
        modelBuilder.Entity<Subscription>()
            .Property(s => s.UserId).HasColumnName("user_id");
        modelBuilder.Entity<Subscription>()
            .Property(s => s.PlanName).HasColumnName("plan_name");
        modelBuilder.Entity<Subscription>()
            .Property(s => s.QuantityMonths).HasColumnName("quantity_months");
        modelBuilder.Entity<Subscription>()
            .Property(s => s.StartsAt).HasColumnName("starts_at");
        modelBuilder.Entity<Subscription>()
            .Property(s => s.ExpiresAt).HasColumnName("expires_at");
        modelBuilder.Entity<Subscription>()
            .Property(s => s.AutoRenew).HasColumnName("auto_renew");
        modelBuilder.Entity<Subscription>()
            .Property(s => s.TransactionId).HasColumnName("transaction_id");

        // --- 4. ANALYTICS & PRACTICE Mappings ---
        modelBuilder.Entity<Analytics>()
            .Property(a => a.UserId).HasColumnName("user_id");
        modelBuilder.Entity<Analytics>()
            .Property(a => a.LessonId).HasColumnName("lesson_id");
        modelBuilder.Entity<Analytics>()
            .Property(a => a.TimeSpentSeconds).HasColumnName("time_spent_seconds");
        modelBuilder.Entity<Analytics>()
            .Property(a => a.CompletedAt).HasColumnName("completed_at");

        modelBuilder.Entity<PracticeSession>()
            .Property(p => p.UserId).HasColumnName("user_id");
        modelBuilder.Entity<PracticeSession>()
            .Property(p => p.XpEarned).HasColumnName("xp_earned");
        modelBuilder.Entity<PracticeSession>()
            .Property(p => p.DurationSeconds).HasColumnName("duration_seconds");
        modelBuilder.Entity<PracticeSession>()
            .Property(p => p.CreatedAt).HasColumnName("created_at");

        // --- 5. AI SESSIONS Mappings ---
        modelBuilder.Entity<AiSession>()
            .Property(a => a.UserId).HasColumnName("user_id");
        modelBuilder.Entity<AiSession>()
            .Property(a => a.LessonId).HasColumnName("lesson_id");
        modelBuilder.Entity<AiSession>()
            .Property(a => a.StartTime).HasColumnName("start_time");
        modelBuilder.Entity<AiSession>()
            .Property(a => a.EndTime).HasColumnName("end_time");
        modelBuilder.Entity<AiSession>()
            .Property(a => a.AiFeedback).HasColumnName("ai_feedback");
        modelBuilder.Entity<AiSession>()
            .Property(a => a.AiScore).HasColumnName("ai_score");

        modelBuilder.Entity<AiMessage>()
            .Property(m => m.SessionId).HasColumnName("session_id");

        // --- 6. ADMIN & LOGS Mappings ---
        modelBuilder.Entity<AdminLog>()
            .Property(l => l.AdminId).HasColumnName("admin_id");
        modelBuilder.Entity<AdminLog>()
            .Property(l => l.ActionType).HasColumnName("action_type");
        modelBuilder.Entity<AdminLog>()
            .Property(l => l.TargetUserId).HasColumnName("target_user_id");

        modelBuilder.Entity<AuditLog>()
            .Property(l => l.EventType).HasColumnName("event_type");
        modelBuilder.Entity<AuditLog>()
            .Property(l => l.UserId).HasColumnName("user_id");
        modelBuilder.Entity<AuditLog>()
            .Property(l => l.CreatedAt).HasColumnName("created_at");

        modelBuilder.Entity<BannedUser>()
            .Property(b => b.UserId).HasColumnName("user_id");
        modelBuilder.Entity<BannedUser>()
            .Property(b => b.BannedUntil).HasColumnName("banned_until");
        modelBuilder.Entity<BannedUser>()
            .Property(b => b.IssuedBy).HasColumnName("issued_by");
        modelBuilder.Entity<BannedUser>()
            .Property(b => b.CreatedAt).HasColumnName("created_at");

        // --- 7. FRIENDSHIPS Mappings ---
        modelBuilder.Entity<Friendship>()
            .Property(f => f.SenderId).HasColumnName("sender_id");
        modelBuilder.Entity<Friendship>()
            .Property(f => f.ReceiverId).HasColumnName("receiver_id");
        modelBuilder.Entity<Friendship>()
            .Property(f => f.CreatedAt).HasColumnName("created_at");

        // --- 8. CHAT MESSAGES Mappings ---
        modelBuilder.Entity<ChatMessage>()
            .Property(m => m.SenderId).HasColumnName("sender_id");
        modelBuilder.Entity<ChatMessage>()
            .Property(m => m.ReceiverId).HasColumnName("receiver_id");
        modelBuilder.Entity<ChatMessage>()
            .Property(m => m.IsDeleted).HasColumnName("is_deleted");
        modelBuilder.Entity<ChatMessage>()
            .Property(m => m.IsReported).HasColumnName("is_reported");

        // --- 9. REPORTS Mappings ---
        modelBuilder.Entity<Report>()
            .Property(r => r.ReporterId).HasColumnName("reporter_id");
        modelBuilder.Entity<Report>()
            .Property(r => r.MessageId).HasColumnName("message_id");
        modelBuilder.Entity<Report>()
            .Property(r => r.ResolvedBy).HasColumnName("resolved_by");
        modelBuilder.Entity<Report>()
            .Property(r => r.CreatedAt).HasColumnName("created_at");

        // --- 10. NOTIFICATIONS Mappings ---
        modelBuilder.Entity<Notification>()
            .Property(n => n.UserId).HasColumnName("user_id");
        modelBuilder.Entity<Notification>()
            .Property(n => n.IsRead).HasColumnName("is_read");
        modelBuilder.Entity<Notification>()
            .Property(n => n.CreatedAt).HasColumnName("created_at");

        // --- UNITS Mappings ---
        modelBuilder.Entity<Unit>().ToTable("units");
        modelBuilder.Entity<Unit>().Property(u => u.CourseId).HasColumnName("course_id");
        modelBuilder.Entity<Unit>().Property(u => u.OrderIndex).HasColumnName("order_index");

        // --- LESSONS Mappings ---
        modelBuilder.Entity<Lesson>().Property(l => l.UnitId).HasColumnName("unit_id");

        modelBuilder.Entity<ApiToken>().ToTable("api_tokens");
        modelBuilder.Entity<ApiToken>().Property(t => t.UserId).HasColumnName("user_id");
        modelBuilder.Entity<ApiToken>().Property(t => t.CreatedAt).HasColumnName("created_at");
        modelBuilder.Entity<ApiToken>().Property(t => t.ExpiresAt).HasColumnName("expires_at");
    }
}
