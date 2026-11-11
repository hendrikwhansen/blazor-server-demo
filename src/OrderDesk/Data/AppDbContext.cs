using Microsoft.EntityFrameworkCore;

namespace OrderDesk.Data;

/// <summary>
/// The DbContext is the "unit of work" against the database - think of it as a
/// SqlConnection + a set of typed DataTables that track changes for you.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Most of the schema comes from conventions and attributes on the entity classes.
        // Anything else can be configured here ("fluent API").

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Name);

        modelBuilder.Entity<Order>(order =>
        {
            // Store the enum as readable text ('New', 'Shipped', ...) instead of 0, 1, 2...
            order.Property(o => o.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Don't allow deleting a customer that still has orders.
            order.HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
