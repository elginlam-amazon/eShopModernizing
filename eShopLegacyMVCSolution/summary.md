# eShopLegacyMVCSolution – .NET Framework → net10.0 upgrade

## Result
`dotnet build eShopLegacyMVC.sln` (and each project individually): 0 errors, 0 warnings.

| Project | Before | After |
|---|---|---|
| eShopLegacy.Utilities | net461 (legacy csproj) | SDK-style, `net10.0;netstandard2.0` |
| eShopPorted | ASP.NET Core 2.2 on net461 | net10.0, generic host + Startup, Autofac 9 |
| src/eShopLegacyMVC | ASP.NET MVC 5 / Web API 2 / EF6 on net472 | ASP.NET Core MVC net10.0, EF Core 10, Autofac 9 |

## What changed
- **eShopLegacy.Utilities / `Serializing`**: `BinaryFormatter` was removed from .NET 9+, so `Serializing` now uses System.Text.Json on both TFMs. The method names are unchanged. I added `DeserializeBinary<T>`; the untyped `DeserializeBinary` now returns a `JsonElement`. **The wire format changes from binary to JSON** (`api/files` responses).
- **eShopLegacyMVC hosting**: `Global.asax` and `App_Start/*` were replaced by `Program.cs` (WebApplication + Autofac `ApplicationModule`).
  - `Session_Start` became `SessionStartMiddleware`.
  - `Application_BeginRequest` (the log4net context properties) became `RequestLogContextMiddleware`.
  - The global `HandleErrorAttribute` became `UseExceptionHandler("/Error")` plus `ErrorController`, used outside Development.
  - The `<globalization>` en-US setting became request localization fixed to en-US.
- **Configuration**: `Web.config` was replaced by `appsettings.json` (`ConnectionStrings:CatalogDBContext`, `UseMockData`, `UseCustomizationData`). `appsettings.Development.json` and `launchSettings.json` were added.
- **EF6 → EF Core 10**:
  - Fluent mappings were ported. The explicit sequence-assigned ids use `ValueGeneratedNever()`, and `Price` is `decimal(18,2)` as before.
  - `SqlQuery` became `SqlQueryRaw(...).AsEnumerable()`, because `NEXT VALUE FOR` cannot be composed into a subquery. `ExecuteSqlCommand` became `ExecuteSqlRaw`.
  - `CreateDatabaseIfNotExists` became `CatalogDBInitializer.InitializeDatabase`, which calls `EnsureCreated()` and then `Seed`. It runs at startup when `UseMockData=false`.
- **Web API 2 → ASP.NET Core controllers**: `BrandsController` and `FilesController` now use attribute routes under `api/[controller]`. JSON naming policy is set to `null`, so responses stay PascalCase as with Json.NET / MVC 5.
- **Logging**: code now uses `ILogger<T>`. In eShopLegacyMVC, log4net 3.5.0 is still the sink, through `Microsoft.Extensions.Logging.Log4Net.AspNetCore` and the same `log4Net.xml`; the log path separator was changed to `/`. In eShopPorted, log4net was never configured, so it now uses the default providers.
- **Static files**: `Content`, `Scripts`, `Images`, `fonts`, `Pics` and `favicon.ico` were moved into `wwwroot` (git mv). `System.Web.Optimization` bundles became explicit `<link>`/`<script>` tags inside `<environment>` blocks: full files in Development, `.min` files elsewhere.
- **Views**: added `_ViewImports.cshtml` with tag helpers, so `~/` resolves. `Html.Partial` became `<partial>`. Session footer now uses `Context.Session.GetString`. Image paths were fixed to match the folder's case (`~/Images/...`), which matters on Linux.
  - `Html.ActionLink(text, action, null, new {...})` now uses named `routeValues:`/`htmlAttributes:` arguments. ASP.NET Core would otherwise pick the `controllerName` overload and emit the CSS class as a query string. I made the same fix in eShopPorted.
- **Packages**: removed the MVC5, Web API, EF6, Optimization, WebGrease, Antlr, ApplicationInsights (classic), Autofac.Mvc5/WebApi2, System.* polyfills and client-side content packages. Pinned `System.Security.Cryptography.Pkcs` 10.0.13 to clear the NU1902/NU1903 advisories on the version pulled in by Microsoft.Data.SqlClient.
- **eShopPorted**: `ConfigureServices` returning `IServiceProvider` became `ConfigureContainer` with `AutofacServiceProviderFactory`. `UseMvc` became endpoint routing. `PicController` was ported off `System.Web.Mvc` and now reads from `wwwroot/Pics`. `FilesController` now returns the serialized stream as the body instead of `Ok(stream)`.

## Next steps
- **Database**:
  - Seeding now runs at startup rather than on first DbContext use, so the app fails fast if SQL Server is unreachable.
  - In a new database, `CatalogType.Id` and `CatalogBrand.Id` are no longer IDENTITY columns. Ids come from the hilo sequences as the initializer code intended, and existing databases are unaffected.
  - The connection string has `TrustServerCertificate=True` for LocalDB (SqlClient 4+ encrypts by default). Override it per environment for production, for example with the `ConnectionStrings__CatalogDBContext` environment variable.
- **`api/files` consumers** must switch from BinaryFormatter to JSON. With `[ApiController]`, 404 responses now carry a ProblemDetails body.
- **`CatalogController2`** (`Controllers/Api`) was never discovered by MVC 5 because its name lacks the `Controller` suffix. ASP.NET Core discovers it through its base class, so `GET /api` now returns `{"Message":"Hello World!"}`. Add `[NonController]` if that is not wanted.
- **Application Insights (classic)** was removed. It had no instrumentation key configured. If telemetry is needed, add `Microsoft.ApplicationInsights.AspNetCore` or OpenTelemetry with a connection string.
- **Bundling**: there is no minification or bundling of `custom.css`, `base.css` and `Site.css`. Consider esbuild (KB 09) if bundling is needed.
- **Logging**: the KB prefers a full move off log4net, for example to Serilog with a file sink. log4net is kept only as the sink to preserve the existing rolling-file output. Note that the existing layout references `%property{activity}`, but the property set is `activityid`. This is a pre-existing mismatch that I left unchanged.
- **eShopPorted, pre-existing gaps (not changed)**:
  - `UseExceptionHandler("/Home/Error")` points at a non-existent HomeController.
  - `wwwroot` has no `Content`/`Pics` folders referenced by its layout and `PicController`.
  - Its EF Core migrations were generated with EF Core 2.2. Regenerate them with `dotnet ef migrations add` before relying on `Migrate()`.
- **Tests**: there are no test projects in the solution. Add integration tests (WebApplicationFactory) for the catalog CRUD, `api/brands` and `api/files` endpoints.
