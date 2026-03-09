using Lynqo_Backend.Models;

public static class HeartService
{
    public const int MaxHearts = 5;

    public static void ApplyHeartRefill(User user)
    {
        var now = DateTime.UtcNow;

        if (user.Hearts >= MaxHearts)
        {
            user.Hearts = MaxHearts;
            user.LastHeartRefillAt = null;
            return;
        }

        if (user.LastHeartRefillAt == null)
        {
            user.LastHeartRefillAt = now;
            return;
        }

        var elapsed = now - user.LastHeartRefillAt.Value;
        var fullHours = (int)Math.Floor(elapsed.TotalHours);

        if (fullHours <= 0) return;

        var heartsToAdd = Math.Min(fullHours, MaxHearts - user.Hearts);
        user.Hearts += heartsToAdd;

        if (user.Hearts >= MaxHearts)
        {
            user.Hearts = MaxHearts;
            user.LastHeartRefillAt = null;
        }
        else
        {
            user.LastHeartRefillAt = user.LastHeartRefillAt.Value.AddHours(heartsToAdd);
        }
    }

    public static bool TrySpendHeart(User user)
    {
        ApplyHeartRefill(user);

        if (user.Hearts <= 0)
            return false;

        user.Hearts--;

        if (user.Hearts < MaxHearts && user.LastHeartRefillAt == null)
        {
            user.LastHeartRefillAt = DateTime.UtcNow;
        }

        return true;
    }
}
