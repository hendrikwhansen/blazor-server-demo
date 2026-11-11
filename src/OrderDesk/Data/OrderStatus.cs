namespace OrderDesk.Data;

public enum OrderStatus
{
    New,
    Confirmed,
    Shipped,
    Cancelled
}

public static class OrderStatusExtensions
{
    /// <summary>Bootstrap badge colour used wherever a status is displayed.</summary>
    public static string BadgeClass(this OrderStatus status) => status switch
    {
        OrderStatus.New => "badge text-bg-primary",
        OrderStatus.Confirmed => "badge text-bg-warning",
        OrderStatus.Shipped => "badge text-bg-success",
        OrderStatus.Cancelled => "badge text-bg-secondary",
        _ => "badge text-bg-light"
    };
}
