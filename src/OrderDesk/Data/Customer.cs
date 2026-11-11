using System.ComponentModel.DataAnnotations;

namespace OrderDesk.Data;

/// <summary>
/// A plain C# class ("entity"). EF Core maps it to the dbo.Customers table.
/// The validation attributes are used both by the database schema (e.g. nvarchar(100))
/// and by the edit form in the UI - one set of rules, two uses.
/// </summary>
public class Customer
{
    public int Id { get; set; }   // Primary key + IDENTITY column, by convention

    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = "";

    [Phone, StringLength(30)]
    public string? Phone { get; set; }   // string? = NULL allowed in the database

    [StringLength(100)]
    public string? City { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // Navigation property: one customer has many orders (foreign key on Order.CustomerId)
    public List<Order> Orders { get; set; } = [];
}
