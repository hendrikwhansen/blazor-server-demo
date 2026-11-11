# OrderDesk – en minimal Blazor Server-demo

En bevidst lille forretningsapplikation, der viser, hvordan en moderne .NET-stak ser ud for udviklere,
der kommer fra **ASP.NET WebForms, .NET Framework og SQL Server**.

Den har to relaterede tabeller (kunder og ordrer) og viser både **visning** af data (lister med
søgning, sortering og paging samt en master/detail-side) og **indtastning** af data (opret/rediger-formularer
med validering og opslags-dropdowns).

| | |
|---|---|
| Runtime | .NET 10 (LTS), C# 14 |
| UI | Blazor Web App, render mode *Interactive Server*, Bootstrap 5 |
| Dataadgang | Entity Framework Core 10, code-first migrations |
| Database | Azure SQL Database |
| Database-login | Microsoft Entra ID, uden password (`Authentication=Active Directory Default`) |

> Dette er eksempelkode, ikke en skabelon til produktion: ingen brugerlogin, ingen tests, ingen CI/CD.
> Selve koden og kommentarerne i den er på engelsk.

## Fra WebForms til Blazor – oversigt

| WebForms | Blazor (dette repo) |
|---|---|
| `Default.aspx` | `Home.razor` – HTML + Razor-syntaks (`@if`, `@foreach`, `@variable`) |
| `Default.aspx.cs` (code-behind) | En `@code { }`-blok **eller** en partial class i `*.razor.cs` – se [CustomerEdit.razor.cs](src/OrderDesk/Components/Pages/Customers/CustomerEdit.razor.cs) |
| `Page_Load` / `if (!IsPostBack)` | `OnInitializedAsync()` |
| `btnSave_Click` | `@onclick="..."` eller `<EditForm OnValidSubmit="...">` |
| Postback + ViewState | Hændelsen sendes over en SignalR-forbindelse (WebSocket); komponentens felter bevarer simpelthen deres værdier på serveren – ingen ViewState |
| UpdatePanel | Ikke nødvendig – hver interaktion opdaterer kun den del af siden, der er ændret |
| `AutoPostBack="true"` | `@bind` / `@bind:event="oninput"` – se søgefeltet og statusfilteret |
| GridView | `QuickGrid` med sortering og paging udført i SQL |
| Repeater | `@foreach` over en liste |
| DropDownList | `InputSelect` / `<select>` |
| RequiredFieldValidator m.fl. | Data annotations (`[Required]`, `[StringLength]`, `[Range]`) på klassen + `DataAnnotationsValidator` |
| User control (`.ascx`) | Enhver `.razor`-fil er en genbrugelig komponent |
| Master page | `Components/Layout/MainLayout.razor` |
| `Response.Redirect` | `NavigationManager.NavigateTo(...)` |
| `Global.asax` + `web.config` | `Program.cs` + `appsettings.json` |
| Statiske filer overalt i site-mappen, skjult via regler i `web.config` | Kun `wwwroot/` er offentlig; konfiguration og kode kan aldrig downloades. `MapStaticAssets()` sørger for caching, komprimering og filnavne, der skifter, når indholdet ændres |
| Sidespecifik CSS i fælles stylesheets | `MyComponent.razor.css` – CSS, der kun gælder for den ene komponent og bundtes automatisk ved build |
| `SqlConnection` / `SqlCommand` / DataSet | EF Core `DbContext` + LINQ |
| Håndvedligeholdte DDL-scripts | EF Core migrations (C#), som kan omsættes til et almindeligt SQL-script |

## Projektstruktur

```
src/OrderDesk/
├── Program.cs                     Opstart: services, EF Core, HTTP-pipeline
├── appsettings.json               Connection string (uden password)
├── wwwroot/                       Statiske filer, der sendes uændret: app.css, favicon, Bootstrap
├── Data/
│   ├── Customer.cs, Order.cs      Entiteter = tabeller
│   ├── AppDbContext.cs            EF Core-context + ekstra mapping
│   ├── SeedData.cs                Demodata ved første opstart
│   └── Migrations/                Genererede schema-migrations
└── Components/
    ├── Layout/                    Master page + navigationsmenu
    └── Pages/
        ├── Home.razor             Dashboard (antal og summer)
        ├── Customers/             Liste (søg/sortér/sider), detaljer med ordrer, opret/rediger
        └── Orders/                Liste med statusfilter, opret/rediger med kunde-dropdown
db/schema.sql                      Migrations som almindeligt T-SQL-script, til orientering
```

## Sådan kører du den

### 1. Forudsætninger

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli), logget ind med `az login`
  (eller logget ind på Azure i Visual Studio)

### 2. Opret en Azure SQL-database (én gang)

Serveren oprettes med **kun Entra-godkendelse** og *dig* som administrator – ingen SQL-logins eller passwords.

```bash
RG=rg-orderdesk
LOCATION=swedencentral
SERVER=orderdesk-$RANDOM          # skal være globalt unikt

az group create --name $RG --location $LOCATION

az sql server create --resource-group $RG --name $SERVER --location $LOCATION \
  --enable-ad-only-auth \
  --external-admin-principal-type User \
  --external-admin-name "$(az ad signed-in-user show --query userPrincipalName -o tsv)" \
  --external-admin-sid "$(az ad signed-in-user show --query id -o tsv)"

# Bruger Azure SQL's gratis tilbud (én gratis serverless-database pr. subscription)
az sql db create --resource-group $RG --server $SERVER --name OrderDesk \
  --edition GeneralPurpose --compute-model Serverless --family Gen5 --capacity 2 \
  --use-free-limit --free-limit-exhaustion-behavior AutoPause

# Giv din egen IP-adresse adgang gennem serverens firewall
MYIP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create --resource-group $RG --server $SERVER \
  --name my-ip --start-ip-address $MYIP --end-ip-address $MYIP

echo "Server name: $SERVER.database.windows.net"
```

### 3. Peg applikationen på databasen

Erstat `YOUR-SERVER-NAME` med dit servernavn i [`src/OrderDesk/appsettings.json`](src/OrderDesk/appsettings.json).
Bemærk, at connection string'en hverken indeholder brugernavn eller password:

```
Server=tcp:YOUR-SERVER-NAME.database.windows.net,1433;Database=OrderDesk;Authentication=Active Directory Default;Encrypt=True;
```

`Active Directory Default` får SqlClient til at hente et token fra den identitet, der er til rådighed:
dit `az login` / din Visual Studio-konto på en udviklermaskine eller den **managed identity**, når
applikationen kører i Azure App Service.

### 4. Kør

```bash
dotnet run --project src/OrderDesk
```

Ved opstart kører applikationen EF Core-migrations (opretter tabellerne) og indsætter lidt
demodata. Den SQL, som EF Core sender til databasen, skrives i konsollen, så du kan se præcis,
hvad hver side gør.

> Intet Azure-abonnement ved hånden? Skift connection string'en til en lokal SQL Server eller LocalDB, f.eks.
> `Server=(localdb)\MSSQLLocalDB;Database=OrderDesk;Trusted_Connection=True;` – koden er uændret.

## Ændringer i databaseskemaet

1. Ret entitetsklassen, f.eks. ved at tilføje en property til `Customer.cs`.
2. Opret en migration (værktøjet `dotnet-ef` er fastlåst i `dotnet-tools.json`):

   ```bash
   dotnet tool restore
   dotnet ef migrations add AddCustomerVatNumber --project src/OrderDesk --output-dir Data/Migrations
   ```

3. Start applikationen (eller kør `dotnet ef database update --project src/OrderDesk`).

Vil du se eller videregive T-SQL'en, kan du generere et idempotent script – sådan blev [`db/schema.sql`](db/schema.sql) lavet:

```bash
dotnet ef migrations script --project src/OrderDesk --idempotent --output db/schema.sql
```

## Udrulning til Azure (skitse)

1. Opret en App Service (Linux, .NET 10) og slå dens **system-assigned managed identity** til.
2. Giv denne identitet adgang til databasen. Forbundet til databasen som Entra-administrator kører du:

   ```sql
   CREATE USER [your-app-service-name] FROM EXTERNAL PROVIDER;
   ALTER ROLE db_datareader ADD MEMBER [your-app-service-name];
   ALTER ROLE db_datawriter ADD MEMBER [your-app-service-name];
   ALTER ROLE db_ddladmin  ADD MEMBER [your-app-service-name];   -- only because the demo migrates on startup
   ```

   (`db_ddladmin` er kun nødvendig, fordi demoen kører migrations ved opstart.)

3. Angiv connection string'en `OrderDesk` i App Service-konfigurationen, og slå **WebSockets** til
   (Blazor Server bruger dem til den levende forbindelse).

## Godt at vide om Blazor Server

- **Én levende forbindelse pr. browserfane.** UI-tilstanden ligger i serverens hukommelse, mens fanen
  er åben. Det fungerer fint til forretningsapplikationer med hundredvis af brugere; til meget store
  offentlige sites vil man overveje andre render modes.
- **Brug `IDbContextFactory`, ikke en langtlevende `DbContext`.** En brugers session kan vare i timevis;
  hver operation i denne demo opretter en kortlivet context. De to listesider beholder én context pr. side,
  fordi QuickGrid forespørger på den, mens du sorterer og bladrer.
- **Prerendering.** Siderne renderes først som statisk HTML (hurtig første visning, SEO) og bliver
  derefter interaktive. Derfor kan `OnInitializedAsync` blive kørt to gange – lad den være fri for sideeffekter.
- **Genforsøg ved forbigående fejl.** `EnableRetryOnFailure()` i `Program.cs` håndterer de korte
  forbindelsesudfald, som clouddatabaser af og til har.
