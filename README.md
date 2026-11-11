# OrderDesk – a minimal Blazor Server demo

A deliberately small business app showing how a modern .NET stack looks to developers coming from
**ASP.NET WebForms, .NET Framework and SQL Server**.

It has two related tables (customers and orders) and shows both **displaying** data (lists with
search, sorting and paging; a master/detail page) and **entering** data (create/edit forms with
validation and lookup dropdowns).

| | |
|---|---|
| Runtime | .NET 10 (LTS), C# 14 |
| UI | Blazor Web App, *Interactive Server* render mode, Bootstrap 5 |
| Data access | Entity Framework Core 10, code-first migrations |
| Database | Azure SQL Database |
| DB login | Microsoft Entra ID, passwordless (`Authentication=Active Directory Default`) |

> This is example code, not a production template: no authentication, no tests, no CI/CD.

## WebForms → Blazor cheat sheet

| WebForms | Blazor (this repo) |
|---|---|
| `Default.aspx` | `Home.razor` – HTML + Razor syntax (`@if`, `@foreach`, `@variable`) |
| `Default.aspx.cs` (code-behind) | `@code { }` block, **or** a partial class in `*.razor.cs` – see [CustomerEdit.razor.cs](src/OrderDesk/Components/Pages/Customers/CustomerEdit.razor.cs) |
| `Page_Load` / `if (!IsPostBack)` | `OnInitializedAsync()` |
| `btnSave_Click` | `@onclick="..."` or `<EditForm OnValidSubmit="...">` |
| Postback + ViewState | Event sent over a SignalR (WebSocket) connection; component fields simply keep their values on the server – no ViewState |
| UpdatePanel | Not needed – every interaction updates only the changed part of the page |
| `AutoPostBack="true"` | `@bind` / `@bind:event="oninput"` – see the search box and status filter |
| GridView | `QuickGrid` with sorting and paging done in SQL |
| Repeater | `@foreach` over a list |
| DropDownList | `InputSelect` / `<select>` |
| RequiredFieldValidator etc. | Data annotations (`[Required]`, `[StringLength]`, `[Range]`) on the class + `DataAnnotationsValidator` |
| User control (`.ascx`) | Any `.razor` file is a reusable component |
| Master page | `Components/Layout/MainLayout.razor` |
| `Response.Redirect` | `NavigationManager.NavigateTo(...)` |
| `Global.asax` + `web.config` | `Program.cs` + `appsettings.json` |
| Static files anywhere in the site folder, hidden via `web.config` rules | Only `wwwroot/` is public; config and code can never be downloaded. `MapStaticAssets()` adds caching, compression and cache-busting file names |
| Page-specific CSS in shared stylesheets | `MyComponent.razor.css` – CSS scoped to that one component, bundled automatically at build |
| `SqlConnection` / `SqlCommand` / DataSet | EF Core `DbContext` + LINQ |
| Hand-maintained DDL scripts | EF Core migrations (C#), which can be turned into a plain SQL script |

## Project layout

```
src/OrderDesk/
├── Program.cs                     Startup: services, EF Core, HTTP pipeline
├── appsettings.json               Connection string (no password)
├── wwwroot/                       Static files served as-is: app.css, favicon, Bootstrap
├── Data/
│   ├── Customer.cs, Order.cs      Entities = tables
│   ├── AppDbContext.cs            EF Core context + extra mapping
│   ├── SeedData.cs                Demo rows on first start
│   └── Migrations/                Generated schema migrations
└── Components/
    ├── Layout/                    Master page + navigation menu
    └── Pages/
        ├── Home.razor             Dashboard (counts and sums)
        ├── Customers/             List (search/sort/page), details with orders, create/edit
        └── Orders/                List with status filter, create/edit with customer dropdown
db/schema.sql                      The migrations as a plain T-SQL script, for reference
```

## Running it

### 1. Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli), signed in with `az login`
  (or be signed in to Azure in Visual Studio)

### 2. Create an Azure SQL database (once)

The server is created with **Entra-only authentication** and *you* as the admin – no SQL logins or passwords.

```bash
RG=rg-orderdesk
LOCATION=swedencentral
SERVER=orderdesk-$RANDOM          # must be globally unique

az group create --name $RG --location $LOCATION

az sql server create --resource-group $RG --name $SERVER --location $LOCATION \
  --enable-ad-only-auth \
  --external-admin-principal-type User \
  --external-admin-name "$(az ad signed-in-user show --query userPrincipalName -o tsv)" \
  --external-admin-sid "$(az ad signed-in-user show --query id -o tsv)"

# Uses the Azure SQL free offer (one free serverless database per subscription)
az sql db create --resource-group $RG --server $SERVER --name OrderDesk \
  --edition GeneralPurpose --compute-model Serverless --family Gen5 --capacity 2 \
  --use-free-limit --free-limit-exhaustion-behavior AutoPause

# Allow your own IP through the server firewall
MYIP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create --resource-group $RG --server $SERVER \
  --name my-ip --start-ip-address $MYIP --end-ip-address $MYIP

echo "Server name: $SERVER.database.windows.net"
```

### 3. Point the app at it

In [`src/OrderDesk/appsettings.json`](src/OrderDesk/appsettings.json) replace `YOUR-SERVER-NAME`
with your server name. Note that the connection string contains no user name or password:

```
Server=tcp:YOUR-SERVER-NAME.database.windows.net,1433;Database=OrderDesk;Authentication=Active Directory Default;Encrypt=True;
```

`Active Directory Default` makes SqlClient obtain a token from whatever identity is available:
your `az login` / Visual Studio sign-in on a developer machine, or the **managed identity** when
the app runs in Azure App Service.

### 4. Run

```bash
dotnet run --project src/OrderDesk
```

On startup the app applies the EF Core migrations (creates the tables) and inserts a few demo
rows. The SQL that EF Core sends to the database is written to the console, so you can see
exactly what each page does.

> No Azure subscription at hand? Swap the connection string for a local SQL Server or LocalDB, e.g.
> `Server=(localdb)\MSSQLLocalDB;Database=OrderDesk;Trusted_Connection=True;` – the code is unchanged.

## Changing the database schema

1. Change the entity class, e.g. add a property to `Customer.cs`.
2. Create a migration (the `dotnet-ef` tool is pinned in `dotnet-tools.json`):

   ```bash
   dotnet tool restore
   dotnet ef migrations add AddCustomerVatNumber --project src/OrderDesk --output-dir Data/Migrations
   ```

3. Run the app (or `dotnet ef database update --project src/OrderDesk`).

To see or hand over the T-SQL, generate an idempotent script – this is how [`db/schema.sql`](db/schema.sql) was made:

```bash
dotnet ef migrations script --project src/OrderDesk --idempotent --output db/schema.sql
```

## Deploying to Azure (outline)

1. Create an App Service (Linux, .NET 10) and enable its **system-assigned managed identity**.
2. Give that identity access to the database. Connected to the database as the Entra admin, run:

   ```sql
   CREATE USER [your-app-service-name] FROM EXTERNAL PROVIDER;
   ALTER ROLE db_datareader ADD MEMBER [your-app-service-name];
   ALTER ROLE db_datawriter ADD MEMBER [your-app-service-name];
   ALTER ROLE db_ddladmin  ADD MEMBER [your-app-service-name];   -- only because the demo migrates on startup
   ```

3. Set the connection string `OrderDesk` in the App Service configuration and enable **WebSockets**
   (Blazor Server uses them for the live connection).

## Things worth knowing about Blazor Server

- **One live connection per browser tab.** UI state lives in server memory while the tab is open.
  Fine for line-of-business apps with hundreds of users; for very large public sites you would
  consider other render modes.
- **Use `IDbContextFactory`, not a long-lived `DbContext`.** A user's session can last hours; each
  operation in this demo creates a short-lived context. The two list pages keep one context per page
  because QuickGrid queries it while you sort and page.
- **Prerendering.** Pages are first rendered as static HTML (fast first paint, SEO), then become
  interactive. That's why `OnInitializedAsync` can run twice – keep it free of side effects.
- **Retry on transient faults.** `EnableRetryOnFailure()` in `Program.cs` handles the brief
  connection drops that cloud databases occasionally have.
