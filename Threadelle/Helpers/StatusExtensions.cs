using Threadelle.Models;

namespace Threadelle.Helpers
{
    public static class StatusExtensions
    {
        public static string Badge(this OrderStatus status) => status switch
        {
            OrderStatus.Pending => "pending",
            OrderStatus.Confirmed => "confirmed",
            OrderStatus.InProgress => "progress",
            OrderStatus.Ready => "ready",
            OrderStatus.Delivered => "delivered",
            OrderStatus.Cancelled => "cancelled",
            _ => "pending"
        };

        public static string Friendly(this OrderStatus status) => status switch
        {
            OrderStatus.InProgress => "In Progress",
            _ => status.ToString()
        };

        public static string Badge(this CustomOrderStatus status) => status switch
        {
            CustomOrderStatus.Pending          => "pending",
            CustomOrderStatus.Reviewing        => "progress",
            CustomOrderStatus.UnderReview      => "progress",
            CustomOrderStatus.Designing        => "progress",
            CustomOrderStatus.InProgress       => "ready",
            CustomOrderStatus.InProduction     => "ready",
            CustomOrderStatus.Accepted         => "confirmed",
            CustomOrderStatus.AwaitingPayment  => "confirmed",   // gold/green — action required
            CustomOrderStatus.ChangesRequested => "pending",     // amber — needs resubmit
            CustomOrderStatus.QualityCheck     => "ready",
            CustomOrderStatus.Packaging        => "ready",
            CustomOrderStatus.Completed        => "delivered",
            CustomOrderStatus.Delivered        => "delivered",
            CustomOrderStatus.Rejected         => "cancelled",
            _ => "pending"
        };

        public static string Friendly(this CustomOrderStatus status) => status switch
        {
            CustomOrderStatus.Pending          => "Pending Review",
            CustomOrderStatus.Reviewing        => "Under Review",
            CustomOrderStatus.UnderReview      => "Under Review",
            CustomOrderStatus.Designing        => "In Design",
            CustomOrderStatus.Accepted         => "Accepted",
            CustomOrderStatus.AwaitingPayment  => "Quote Ready — Awaiting Payment",
            CustomOrderStatus.ChangesRequested => "Changes Requested",
            CustomOrderStatus.InProgress       => "In Production",
            CustomOrderStatus.InProduction     => "In Production",
            CustomOrderStatus.QualityCheck     => "Quality Check",
            CustomOrderStatus.Packaging        => "Packaging",
            CustomOrderStatus.Delivered        => "Delivered",
            CustomOrderStatus.Completed        => "Completed",
            CustomOrderStatus.Rejected         => "Rejected",
            _ => status.ToString()
        };

        /// <summary>CSS colour variable for card accents on user-facing pages.</summary>
        public static string AccentColor(this CustomOrderStatus status) => status switch
        {
            CustomOrderStatus.AwaitingPayment  => "#22c55e",
            CustomOrderStatus.ChangesRequested => "#f59e0b",
            CustomOrderStatus.Rejected         => "#ef4444",
            CustomOrderStatus.Delivered        => "#3b82f6",
            CustomOrderStatus.Completed        => "#8b5cf6",
            CustomOrderStatus.InProduction     => "#C97B8E",
            CustomOrderStatus.InProgress       => "#C97B8E",
            CustomOrderStatus.QualityCheck     => "#C97B8E",
            CustomOrderStatus.Packaging        => "#C97B8E",
            _ => "var(--te-dusty-rose)"
        };

        public static string Badge(this OrderPaymentStatus status) => status switch
        {
            OrderPaymentStatus.Paid => "delivered",
            OrderPaymentStatus.Refunded => "cancelled",
            _ => "pending"
        };
    }
}
