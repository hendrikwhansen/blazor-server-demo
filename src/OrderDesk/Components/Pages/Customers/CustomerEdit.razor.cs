using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using OrderDesk.Data;

namespace OrderDesk.Components.Pages.Customers;

// "Code-behind" - just like Default.aspx.cs in WebForms.
// The class is partial and is merged with the markup in CustomerEdit.razor at compile time.
// (The other pages keep their code in an @code { } block instead - both styles are fine.)
public partial class CustomerEdit
{
    // Services are injected by the framework (dependency injection).
    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    // Route parameter: null for /customers/new, a number for /customers/42/edit.
    [Parameter] public int? Id { get; set; }

    // Page state. No ViewState: the component instance stays alive on the server
    // for as long as the user has the page open, so fields simply keep their values.
    private Customer? customer;
    private bool saving;

    private bool IsNew => Id is null;

    protected override async Task OnInitializedAsync()
    {
        if (IsNew)
        {
            customer = new Customer();
            return;
        }

        await using var db = await DbFactory.CreateDbContextAsync();
        customer = await db.Customers.FindAsync(Id);

        if (customer is null)
        {
            Navigation.NotFound();
        }
    }

    // The "Save button click handler". Only called when validation has passed.
    private async Task SaveAsync()
    {
        saving = true;

        await using var db = await DbFactory.CreateDbContextAsync();

        if (IsNew)
        {
            db.Customers.Add(customer!);       // INSERT
        }
        else
        {
            db.Customers.Update(customer!);    // UPDATE ... WHERE Id = @id
        }

        await db.SaveChangesAsync();           // Runs the SQL in a transaction

        // The equivalent of Response.Redirect - but without a full page reload.
        Navigation.NavigateTo($"customers/{customer!.Id}");
    }
}
