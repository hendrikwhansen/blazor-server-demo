using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace OrderDesk.Data;

public class Order
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please select a customer.")]
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateOnly OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);   // SQL type: date

    [Required, StringLength(200)]
    public string Description { get; set; } = "";

    [Range(0.01, 1_000_000, ErrorMessage = "Amount must be between 0.01 and 1,000,000.")]
    [Precision(18, 2)]   // SQL type: decimal(18,2)
    public decimal Amount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.New;
}
