# NO23 Sports Club

NO23 Sports Club is an ASP.NET Core MVC application backed by PostgreSQL.

## Local Setup

1. Install .NET 10 SDK and Docker Desktop.
2. Restore local .NET tools:
   ```bash
   dotnet tool restore
   ```
3. Start PostgreSQL:
   ```bash
   docker compose up -d
   ```
4. Configure the local application connection string with user-secrets:
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=no23db;Username=no23;Password=change_me" --project src/NO23.Web/NO23.Web.csproj
   ```
5. Optional default admin seed:
   ```bash
   dotnet user-secrets set "SeedAdmin:Email" "admin@no23.local" --project src/NO23.Web/NO23.Web.csproj
   dotnet user-secrets set "SeedAdmin:Password" "Change_me_123!" --project src/NO23.Web/NO23.Web.csproj
   ```
6. Apply migrations and run the app:
   ```bash
   dotnet ef database update --project src/NO23.Web/NO23.Web.csproj
   dotnet run --project src/NO23.Web/NO23.Web.csproj
   ```

PostgreSQL local connection values:

- Host: `localhost`
- Port: `5433`
- Database: `no23db`
- Username: `no23`
- Password: `change_me`
