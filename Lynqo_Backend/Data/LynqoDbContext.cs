using Microsoft.EntityFrameworkCore;
using Lynqo_Backend.Models;
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



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Table name mappings (SQL uses plural snake_case)
        modelBuilder.Entity<UserXp>().ToTable("user_xp");
        modelBuilder.Entity<Leaderboard>().ToTable("leaderboards");
        modelBuilder.Entity<LeaderboardEntry>().ToTable("leaderboard_entries");
        modelBuilder.Entity<Badge>().ToTable("badges");
        modelBuilder.Entity<UserBadge>().ToTable("user_badges");


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
    }
}
