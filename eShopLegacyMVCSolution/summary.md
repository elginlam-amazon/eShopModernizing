# eShopLegacyMVCSolution – .NET Framework → net10.0 upgrade

## Result
`dotnet build` succeeds (exit 0, **0 errors, 0 warnings**) for `eShopLegacy.Utilities`, `eShopPorted`, `src/eShopLegacyMVC` and `eShopLegacyMVC.sln`.

## Changes per project

### eShopLegacy.Utilities (`net10.0;netstandard2.0`)
- SDK-style, multi-targeted. `GenerateAssemblyInfo=false` keeps `Properties/AssemblyInfo.cs`. Removed the unused `Microsoft.CSharp` and `System.Data.DataSetExtensions` packages.
- `Serializing`: the `netstandard2.0` build still uses `BinaryFormatter`, so .NET Framework callers see no change. On `net10.0`, `BinaryFormatter` is gone from the runtime and always throws, so that build writes UTF-8 JSON with `System.Text.Json`. `DeserializeBinary` now returns a `JsonElement`. **This changes the payload format** returned by the `api/files` endpoints.

### src/eShopLegacyMVC (ASP.NET MVC 5 + Web API 2 + EF6 → ASP.NET Core MVC + EF Core 10)
- SDK-style Web project. Removed `packages.config`, `Global.asax`, `App_Start/*`, `Web*.config`, `Views/Web.config`, `ApplicationInsights.config` and `log4Net.xml`.
- `Program.cs` takes over from Global.asax/App_Start:
  - Autofac (`ApplicationModule` unchanged except that it no longer registers the DbContext).
  - `AddDbContext`.
  - In-proc session; a middleware sets `MachineName`/`SessionStartTime` the way `Session_Start` did.
  - en-US request localization, which replaces `<globalization>`.
  - `UseExceptionHandler("/Error")` (new `ErrorController`), which replaces `HandleErrorAttribute`.
  - Attribute routes plus the `Default` conventional route.
  - A request logging scope, which replaces log4net's `BeginRequest` properties.
- Configuration moved to `appsettings.json` (`ConnectionStrings:CatalogDBContext`, `UseMockData`, `UseCustomizationData`) and `appsettings.Development.json`.
- EF6 → EF Core:
  - `CatalogDBContext` uses the Fluent API with the same tables and keys.
  - `CatalogType.Id` and `CatalogBrand.Id` stay identity columns with the client value ignored on insert, as in EF6.
  - `Price` is mapped as `decimal(18,2)`.
  - `SqlQuery` → `SqlQueryRaw(...).AsEnumerable()`, because `NEXT VALUE FOR` cannot be composed into another query. Sequence names are allow-listed.
  - `CatalogDBInitializer` gets `InitializeDatabase`, which does what `CreateDatabaseIfNotExists` did: `EnsureCreated()` and seed only when the database is new. It runs at startup when `UseMockData=false`. `ConfigurationManager`/`HostingEnvironment` → `IConfiguration`/`IWebHostEnvironment`.
- Controllers:
  - log4net → `ILogger<T>`.
  - `HttpStatusCodeResult`/`HttpNotFound` → `BadRequest()`/`NotFound()`, and `Bind(Include=…)` → `Bind("…")`.
  - `Server.MapPath("~/Pics")` → `ContentRootPath/Pics`.
  - The Web API `BrandsController` and `FilesController` are now `ControllerBase` with attribute routes under the same URLs. JSON keeps PascalCase (`PropertyNamingPolicy = null`) to keep the old API contract.
  - `CatalogController2` is marked `[NonController]`. MVC 5 never discovered it because its name has no `Controller` suffix.
- Views:
  - Static assets moved to `wwwroot`, with `Images` → `images` to match the lower-case references on Linux.
  - `Styles/Scripts.Render` bundles → explicit tags: un-minified in Development, `.min` otherwise, with `asp-append-version`. Added `_ViewImports.cshtml` and `_ValidationScriptsPartial.cshtml`.
  - `HttpContext.Current.Session` → `Context.Session`.
  - `Html.Partial` → `PartialAsync`.
- `System.Security.Cryptography.Pkcs` is pinned to 10.0.13. The 9.0.11 version pulled in by `Microsoft.Data.SqlClient` has the NU1902/NU1903 advisories.

### eShopPorted (net461/ASP.NET Core 2.2 → net10.0)
- Removed packages that only resolved through the .NET Framework fallback (NU1701): `Autofac.Mvc5`, `WebGrease` and `Antlr4`. Also removed vulnerable `log4net` 2.0.10 and the unnecessary `Microsoft.CSharp`. Autofac, EF Core and `Pkcs` are on current versions.
- Hosting:
  - `WebHost` → generic `Host`.
  - The Autofac setup moved from `IServiceProvider ConfigureServices` to `ConfigureContainer`.
  - `UseMvc` → endpoint routing, and `IHostingEnvironment` → `IWebHostEnvironment`.
- `PicController` used `System.Web.Mvc` and has been rewritten for ASP.NET Core. Its empty picture path now reads from `wwwroot/Pics`.
- `FilesController` returns the serialized stream as a file. It used to JSON-serialize the `MemoryStream`, which throws.
- Logging moved from log4net to `ILogger<T>`. Removed `Views/Web.config` and `app.config`; the Web SDK already turns on server GC.

## Behavioural changes to review
- **Logging:** log4net's rolling file `logFiles\myapp.log` is replaced by the built-in providers (console/debug). Request activity id and request info now appear as logger scopes.
- **`api/files` payload:** on net10 it is JSON instead of a BinaryFormatter blob, because the runtime no longer supports BinaryFormatter.
- **Application Insights:** the classic HTTP modules were removed. No instrumentation key was configured in the repo.
- **SQL connection string:** `TrustServerCertificate=True` was added. Microsoft.Data.SqlClient now encrypts by default, which breaks LocalDB without this.

## Next steps
- Production: supply `ConnectionStrings__CatalogDBContext` (and `UseMockData`/`UseCustomizationData`) through environment-specific configuration or secrets. Drop `TrustServerCertificate=True` for real SQL Server hosts that have trusted certificates.
- Decide whether file logging is needed (for example Serilog file sink, see KB doc 12) and whether to add `Microsoft.ApplicationInsights.AspNetCore`.
- Confirm that no external client of `GET api/files` depends on the BinaryFormatter format.
- `eShopPorted`: its migrations were generated with EF Core 2.2. Run `dotnet ef migrations add` to refresh the snapshot before using `Migrate()`, because EF 9+ reports pending model changes. `UseExceptionHandler("/Home/Error")` points to a `HomeController` that does not exist (this was already the case before the upgrade).
- Security, already present before the upgrade: `PicController` combines a database-stored `PictureFileName` into a file path. Consider checking that it stays inside the `Pics` folder.
- The solution has no test projects. Add integration tests (for example `WebApplicationFactory` with `UseMockData=true`) for the catalog CRUD and the API endpoints.
- The application was not run in this cycle (by instruction), and no SQL Server was available. Runtime database seeding and session behaviour still need to be checked in a dev environment.
