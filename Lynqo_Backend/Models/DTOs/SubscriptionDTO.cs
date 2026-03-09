namespace LynqoBackend.Models.DTOs
{
    public class SubscriptionDTO
    {
        public int Id { get; set; }
        public string PlanName { get; set; } = "";
        public int QuantityMonths { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool AutoRenew { get; set; }
    }

    public class StartSubscriptionRequest
    {
        public string PlanName { get; set; } = "Premium";
        public int QuantityMonths { get; set; } = 1;

        // Ezt a sort adtuk hozzá, hogy a React át tudja küldeni a kapcsoló állapotát!
        public bool AutoRenew { get; set; } = true;
    }
}