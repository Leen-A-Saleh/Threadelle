using Threadelle.Models;

namespace Threadelle.ViewModels
{
    public class CustomOrderDetailsViewModel
    {
        public CustomOrder Order { get; set; } = null!;

        // ── Computed status helpers — views carry zero C# logic ──────────────
        public bool IsPendingReview    => Order.Status is CustomOrderStatus.Pending
                                                        or CustomOrderStatus.Reviewing
                                                        or CustomOrderStatus.UnderReview;
        public bool IsChangesRequested => Order.Status == CustomOrderStatus.ChangesRequested;
        public bool IsAwaitingPayment  => Order.Status == CustomOrderStatus.AwaitingPayment;
        public bool IsInProduction     => Order.Status is CustomOrderStatus.Designing
                                                        or CustomOrderStatus.InProduction
                                                        or CustomOrderStatus.InProgress
                                                        or CustomOrderStatus.QualityCheck
                                                        or CustomOrderStatus.Packaging;
        public bool IsDelivered        => Order.Status == CustomOrderStatus.Delivered;
        public bool IsCompleted        => Order.Status == CustomOrderStatus.Completed;
        public bool IsRejected         => Order.Status == CustomOrderStatus.Rejected;
        public bool IsFinished         => IsDelivered || IsCompleted;

        // ── Display helpers ───────────────────────────────────────────────────
        public string CurrentStage          { get; set; } = "Pending Review";
        public bool   RequiresCustomerAction => IsChangesRequested || IsAwaitingPayment;

        // ── Production tracker ────────────────────────────────────────────────
        public List<string> ProductionStages { get; set; } = new()
        {
            "Design", "Materials", "Crafting", "Quality Check", "Packaging", "Completed"
        };

        public string? ActiveProductionStage { get; set; }

        public int ActiveStageIndex =>
            ActiveProductionStage == null ? 0 : Math.Max(0, ProductionStages.IndexOf(ActiveProductionStage));

        /// <summary>Progress bar fill width 0-100 for the production line.</summary>
        public int ProgressLinePercent
        {
            get
            {
                var idx = ActiveStageIndex;
                if (ProductionStages.Count <= 1) return 0;
                return (int)Math.Round(idx * 100.0 / (ProductionStages.Count - 1));
            }
        }
    }
}
