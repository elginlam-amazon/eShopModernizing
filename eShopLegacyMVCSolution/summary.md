# .NET Framework → net10.0 upgrade summary

All three projects build with 0 errors and 0 warnings (`dotnet build`, including `--no-incremental`).

| Project | Before | After |
| --- | --- | --- |
| eShopLegacy.Utilities | net461 class library | SDK-style, `net10.0;netstandard2.0` |
| eShopPorted | ASP.NET Core 2.2 on net461 | ASP.NET Core on net10.0 |
| src/eShopLegacyMVC | ASP.NET MVC 5 + Web API 2 + EF6 on net472 | ASP.NET Core MVC on net10.0, EF Core 10 |

## Key changes

- **Utilities / `Serializing`**: the netstandard2.0 build still uses `BinaryFormatter`, so .NET Framework callers are unaffected. The net10.0 build uses System.Text.Json, because `BinaryFormatter` no longer exists in .NET 9+. `DeserializeBinary` now returns a `JsonElement` on net10.0.
- **eShopLegacyMVC**:
  - `Global.asax` and `App_Start/*` are replaced by `Program.cs`. It covers Autofac with the existing `ApplicationModule`, the routes, session (`Session_Start` values), the log4net `activityid`/`requestinfo` properties, the en-US culture, and the error page (`ErrorController` + `Views/Shared/Error.cshtml` in non-Development environments).
  - EF6 is replaced by EF Core:
    - Fluent mappings are kept. Brand and type keys are still IDENTITY columns and ignore client-assigned values, as in EF6. Catalog item ids still come from the HiLo sequence.
    - `CreateDatabaseIfNotExists` is replaced by `CatalogDBInitializer.InitializeDatabase` (`EnsureCreated` + seed), which runs at startup when `UseMockData=false`.
    - I checked the generated DDL against the EF6 schema without connecting to a database.
  - The Web API 2 controllers are now `ControllerBase` with attribute routes `api/[controller]`. JSON keeps PascalCase property names, as Web API 2 and MVC 5 produced.
  - `Web.config` settings moved to `appsettings.json`. Static assets moved to `wwwroot`. The `System.Web.Optimization` bundles are replaced by explicit `<link>`/`<script>` tags: unminified files in Development, `.min` files with `asp-append-version` in other environments.
  - `Server.MapPath` / `HostingEnvironment` are replaced by `IWebHostEnvironment`. Paths no longer use Windows `\` separators, and `images` references now match the `Images` folder name, because Linux paths are case-sensitive.
  - Removed packages: Microsoft.AspNet.*, EntityFramework, Autofac.Mvc5/WebApi2, Application Insights 2.9 (no instrumentation key was configured), WebGrease, Antlr, the System.* polyfills, and the client-side NuGet packages (their files are already in `wwwroot`).
- **eShopPorted**:
  - Moved to the generic host and endpoint routing. Autofac now uses `ConfigureContainer`.
  - `PicController` no longer uses `System.Web.Mvc` and reads files from `wwwroot/Pics`.
  - Added `_ViewImports.cshtml`, so `~/` URLs resolve again.
  - Removed `Views/Web.config` and `app.config`. Server GC is the Web SDK default.
- **Both web apps**:
  - `Html.Partial` became `PartialAsync`.
  - The `ActionLink(text, action, null, htmlAttributes)` calls now use named arguments. Without them, ASP.NET Core picks the `controllerName` overload and the CSS classes end up in the URL as query values.
  - `System.Security.Cryptography.Pkcs` is pinned to 10.0.13 to clear NU1902/NU1903.

## Next steps

- `/api/files` (both apps) now returns JSON bytes instead of a BinaryFormatter payload. Any client that deserializes it with BinaryFormatter must change.
- The database is now created and seeded at application startup, not on first use. Startup fails if SQL Server can't be reached and `UseMockData=false`.
- `TrustServerCertificate=True` was added to the LocalDB connection string because Microsoft.Data.SqlClient encrypts connections by default. Override the connection string with a properly secured one in test and prod (environment variable or secrets).
- The sequence scripts still contain `USE [Microsoft.eShopOnContainers.Services.CatalogDb]`, as before, so they only work with that database name.
- Bundling/minification: there is no compile-time bundler (esbuild/webpack/vite) yet; the files are served individually. Decide whether to add one, or `MapStaticAssets` for precompression.
- log4net was kept (upgraded to 3.5.0) to preserve the `log4Net.xml` rolling-file setup. Consider moving to `Microsoft.Extensions.Logging`.
- Autofac was kept (existing modules). It could be replaced with the built-in DI.
- In eShopPorted, `UseExceptionHandler("/Home/Error")` points to a controller that does not exist. This was already the case before the upgrade.
- Application Insights was removed. Add `Microsoft.ApplicationInsights.AspNetCore` or OpenTelemetry if telemetry is needed.
- There are no test projects in the solution. Nothing was run against SQL Server, and the apps were not started.
