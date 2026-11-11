// Program.cs replaces Global.asax and most of web.config: it registers services
// (dependency injection) and configures the HTTP pipeline.

using Microsoft.EntityFrameworkCore;
using OrderDesk.Components;
using OrderDesk.Data;

var builder = WebApplication.CreateBuilder(args);

// Blazor with "Interactive Server" rendering: UI events are sent to the server over
// a SignalR (WebSocket) connection, the C# code runs on the server, and only the
// changed HTML is sent back to the browser. Think UpdatePanel - but for the whole app.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// EF Core against Azure SQL. The connection string lives in appsettings.json and uses
// "Authentication=Active Directory Default" - no password anywhere.
// A DbContext *factory* is used because a Blazor Server user session (a "circuit") lives
// for a long time; each operation creates a short-lived DbContext from the factory.
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("OrderDesk"),
        sql => sql.EnableRetryOnFailure()));   // Azure SQL can have brief transient faults - retry automatically

// Lets the QuickGrid component page/sort directly in SQL via EF Core.
builder.Services.AddQuickGridEntityFrameworkAdapter();

var app = builder.Build();

// Create/upgrade the database schema and insert demo data at startup.
// Convenient for a demo; in production you would apply migrations as a deployment step.
await using (var db = await app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync())
{
    await db.Database.MigrateAsync();
    await SeedData.InitializeAsync(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
