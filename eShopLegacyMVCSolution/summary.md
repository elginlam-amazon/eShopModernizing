# .NET Framework → net10.0 migration summary

Build status: `dotnet build` on `eShopLegacy.Utilities`, `eShopPorted`, `src/eShopLegacyMVC` and `eShopLegacyMVC.sln` → 0 errors, 0 warnings (Debug and Release).

## eShopLegacy.Utilities
- SDK-style, `<TargetFrameworks>net10.0;netstandard2.0</TargetFrameworks>`; `System.Text.Json` package only on the netstandard2.0 leg.
- `Serializing` used `BinaryFormatter`, which is removed in .NET 9+ (it always throws) and is insecure. It now writes and reads UTF-8 JSON on both TFMs. The method names are unchanged. `DeserializeBinary(Stream)` now returns a `JsonElement`, and there is a new typed `DeserializeBinary<T>`. **Wire-format change:** callers that expect BinaryFormatter bytes must change.

## eShopLegacyMVC (ASP.NET MVC 5 + Web API 2 + EF6 → ASP.NET Core MVC + EF Core 10)
- Project: SDK Web project. The `packages.config`/HintPath references, ApplicationInsights, WebGrease/Optimization, Antlr and the jQuery/bootstrap content packages are gone. The packages left are Autofac 9.3.4, Autofac.Extensions.DependencyInjection 11.0.2, log4net 3.5.0, EF Core SqlServer 10.0.13, and System.Security.Cryptography.Pkcs 10.0.13, which is pinned to override a vulnerable transitive version.
- `Global.asax` + `App_Start/*` → `Program.cs`:
  - Autofac stays the container (`ApplicationModule` is unchanged except that the DbContext is now registered with `AddDbContext`).
  - The EF6 initializer now runs at startup. It is skipped when `UseMockData` is true.
  - `Application_BeginRequest` (log4net `LogicalThreadContext` properties) → middleware.
  - `Session_Start` (MachineName and SessionStartTime) → session middleware (`AddSession`, in-memory).
  - `<globalization culture="en-US">` → `UseRequestLocalization` (en-US only).
  - `HandleErrorAttribute` → `UseExceptionHandler("/Error")` + `ErrorController` outside Development.
  - Routes: the conventional route plus attribute routes.
- `Web.config` → `appsettings.json` (connection string `CatalogDBContext`, `UseMockData`, `UseCustomizationData`).
- Web API controllers → `[ApiController]` with routes `api/brands`, `api/brands/{id}` and `api/files`. JSON keeps PascalCase (`PropertyNamingPolicy = null`) to match the old Json.NET/MVC output.
- EF6 → EF Core:
  - Fluent configuration ported. Brand and type Ids stay identity columns and client-set values are ignored, as in EF6. Price is `decimal(18,2)`.
  - `CreateDatabaseIfNotExists`+`Seed` → `InitializeDatabase()`, which calls `EnsureCreated()` and seeds only when it created the database.
  - `SqlQuery` → `SqlQueryRaw(...).AsEnumerable()` and `ExecuteSqlCommand` → `ExecuteSqlRaw`. The SQL script paths no longer use Windows backslashes.
- Static assets were moved to `wwwroot`, and `Images` was renamed to lowercase `images` to match the views and CSS on case-sensitive file systems. Bundles were replaced by `<link>`/`<script>` tags: non-minified files in Development, `.min` files with `asp-append-version` elsewhere.
- Views: added `_ViewImports.cshtml`; `HttpContext.Current.Session` → `Context.Session.GetString`; `Html.Partial` → `PartialAsync`. `ActionLink(text, action, null, attrs)` now uses named arguments, because in ASP.NET Core a `null` third argument binds to the `controllerName` overload. The same view fix was applied in eShopPorted.
- log4net is kept: the config attribute filename is now case-correct and the log file path is OS-neutral.

## eShopPorted (ASP.NET Core 2.2 on net461 → net10.0)
- Removed packages that only work on .NET Framework (Autofac.Mvc5, WebGrease, Antlr4, Microsoft.CSharp, Newtonsoft.Json). Autofac, log4net and the Pkcs pin match eShopLegacyMVC.
- `WebHost` → `Host.CreateDefaultBuilder` + `AutofacServiceProviderFactory`. `ConfigureServices` no longer returns an `IServiceProvider`; registrations go through `ConfigureContainer`. `UseMvc` → endpoint routing; `IHostingEnvironment` → `IWebHostEnvironment`.
- `PicController` moved off `System.Web.Mvc`. It now reads from `wwwroot/Pics`; before, the path was empty, so the endpoint was broken.

## Next steps
- Not verified: the app was not run against SQL Server, no test project exists, and no tests were run. Run a smoke test of the catalog CRUD, `/items/{id}/pic`, `/api/brands`, `/api/files` and the session footer.
- Database initialization now runs eagerly at startup. With EF6 it ran lazily on first use, so an unreachable database now stops startup. `EnsureCreated` does not check model compatibility the way EF6 did. Consider EF Core migrations.
- The sequence scripts still contain `USE [Microsoft.eShopOnContainers.Services.CatalogDb]`. The database name in the connection string must match, which was already true before the migration.
- Microsoft.Data.SqlClient defaults to `Encrypt=True`. Non-localdb environments may need `TrustServerCertificate`/`Encrypt` settings in the connection string. Set per environment with `ConnectionStrings__CatalogDBContext`.
- Application Insights was dropped: no instrumentation key was configured. If telemetry is needed, add `Microsoft.ApplicationInsights.AspNetCore`/Azure Monitor with a connection string.
- Web API XML content negotiation is no longer available (JSON only). `GET /api` (`CatalogController2`) now returns JSON. In MVC 5 it was blocked because it lacked `JsonRequestBehavior.AllowGet`.
- Session is in-memory (same as InProc). Multi-instance deployments need a distributed cache.
- Optional: replace log4net with `Microsoft.Extensions.Logging` (KB 12). The existing `log4Net.xml` pattern references `%property{activity}` while the code sets `activityid`; this mismatch was there before the migration.
- eShopPorted (unchanged): `UseExceptionHandler("/Home/Error")` points to a controller that does not exist. Its views and CSS reference `~/images/...` while the folder is `wwwroot/Images`, which fails on Linux. Its EF Core 2.2 migrations were not regenerated.
