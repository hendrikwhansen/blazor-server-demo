using Microsoft.EntityFrameworkCore;

namespace OrderDesk.Data;

/// <summary>Adds a few demo rows the first time the app starts against an empty database.</summary>
public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        if (await db.Customers.AnyAsync())
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);

        db.Customers.AddRange(
            new Customer
            {
                Name = "Nordic Bikes A/S", Email = "info@nordicbikes.example", Phone = "+45 11 22 33 44", City = "Aarhus",
                Orders =
                [
                    new Order { OrderDate = today.AddDays(-30), Description = "20 city bikes", Amount = 59_800m, Status = OrderStatus.Shipped },
                    new Order { OrderDate = today.AddDays(-2), Description = "Spare parts", Amount = 4_250.50m, Status = OrderStatus.Confirmed }
                ]
            },
            new Customer
            {
                Name = "Harbour Coffee ApS", Email = "orders@harbourcoffee.example", City = "Copenhagen",
                Orders =
                [
                    new Order { OrderDate = today.AddDays(-1), Description = "Espresso machine service", Amount = 1_899m, Status = OrderStatus.New }
                ]
            },
            new Customer
            {
                Name = "Fjord Software", Email = "hello@fjordsoftware.example", Phone = "+45 55 66 77 88", City = "Odense",
                Orders =
                [
                    new Order { OrderDate = today.AddDays(-12), Description = "Annual licence renewal", Amount = 24_000m, Status = OrderStatus.Cancelled }
                ]
            },
            new Customer { Name = "Green Gardens", Email = "contact@greengardens.example", City = "Aalborg" });

        await db.SaveChangesAsync();
    }
}
