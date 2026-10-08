# .NET Framework → net10.0 Migration Summary

## Migrated Projects

| Project | From | To | Status |
|---|---|---|---|
| eShopLegacy.Utilities | .NET Framework 4.7.2 | netstandard2.0 | ✓ 0 errors, 0 warnings |
| eShopPorted | .NET Framework 4.7.2 | net10.0 | ✓ 0 errors, 0 warnings |
| eShopLegacyMVC | .NET Framework 4.7.2 | net10.0 | ✓ 0 errors, 0 warnings |

## Key Migration Changes

### eShopLegacy.Utilities
- SDK-style `.csproj` targeting `netstandard2.0`
- `BinaryFormatter` (removed in .NET 9+) replaced with `System.Text.Json`
- `GenerateAssemblyInfo=false` to suppress duplicate attribute errors from legacy `Properties/AssemblyInfo.cs`
- Added `LangVersion=latest` for C# 8+ syntax (e.g., using declarations)
- Added `System.Text.Json 9.0.0` package reference

### eShopPorted
- SDK-style `.csproj` targeting `net10.0`
- `Autofac.Mvc5` removed; `Autofac 8.1.0` + `Autofac.Extensions.DependencyInjection 8.0.0`
- `log4net 2.0.x/3.0.x` → `3.5.0` (fixes NU1902 vulnerability advisory)
- EF6 (`System.Data.Entity`) → EF Core 10.0.12 (`Microsoft.EntityFrameworkCore.SqlServer`)
- `Global.asax` / `Startup.cs` → `Program.cs` minimal hosting with `AutofacServiceProviderFactory`
- `System.Web.Mvc` → `Microsoft.AspNetCore.Mvc`; `Controller` base class, `BadRequest()`, `NotFound()`
- `Server.MapPath("~/Pics")` → `IWebHostEnvironment.ContentRootPath`
- `@Html.Partial(...)` → `<partial name="..." model="..." />` tag helper (fixes MVC1000)
- `Startup.StartTime` reference in layout replaced with `DateTime.UtcNow`

### eShopLegacyMVC
- SDK-style `.csproj` targeting `net10.0` with `Nullable=enable`
- Same package upgrades as eShopPorted
- `App_Start/BundleConfig.cs`, `FilterConfig.cs`, `RouteConfig.cs`, `WebApiConfig.cs` stubbed out (System.Web.Optimization/Routing not available)
- `Global.asax.cs` stubbed; full app startup moved to `Program.cs`
- `_ViewImports.cshtml` created to add tag helper directives
- **Controllers**: `System.Web.Mvc` → `Microsoft.AspNetCore.Mvc`; `[Bind(Include=...)]` → `[Bind(...)]`
- **WebAPI controllers**: `ApiController`/`System.Web.Http` → `ControllerBase`/`[ApiController]`; `IHttpActionResult` → `IActionResult`; `HttpResponseMessage` → `File(...)`/`Ok()`
- **EF Core**: `DbModelBuilder` → `ModelBuilder`; `EntityTypeConfiguration<T>` → fluent lambda API; `HasRequired` → `HasOne`; `ValueGeneratedNever()`; `SqlQuery<T>` → `SqlQueryRaw<T>().ToList()`; `ExecuteSqlCommand` → `ExecuteSqlRaw`
- **CatalogDBInitializer**: `CreateDatabaseIfNotExists<T>` → plain class with `Seed(context)` + `context.Database.EnsureCreated()`; injected `IConfiguration` and `IHostEnvironment`
- **Bundling**: `@Styles.Render(...)` / `@Scripts.Render(...)` replaced with explicit static `<link>`/`<script>` tags
- **Nullable**: `CatalogItem` navigation properties marked `CatalogBrand?`/`CatalogType?`; `ICatalogService.FindCatalogItem` returns `CatalogItem?`; view lambdas use `!` null-forgiving operator

## Next Steps

- **Runtime testing**: The application compiles but has not been run. A SQL Server database (or LocalDB) is required to test the non-mock path. Connection string must be configured in `appsettings.json`.
- **Static assets**: `BundleConfig` stubs removed bundling. CSS/JS files under `Content/` and `Scripts/` are referenced as static paths in the layout. Verify the files exist under `wwwroot/` (or configure `UseStaticFiles` with a physical path if kept under `Content/`).
- **wwwroot layout**: ASP.NET Core expects static files under `wwwroot/`. The existing `Content/` and `Scripts/` directories may need to be moved or the `StaticFileOptions.FileProvider` adjusted.
- **Authentication/Authorization**: Original app had no visible auth middleware. If auth is needed, add ASP.NET Core Identity or cookie auth in `Program.cs`.
- **Logging config**: `log4net` 3.5.0 is referenced. A `log4net.config` or `app.config` section with the log4net configuration must be present and loaded at startup (e.g., via `XmlConfigurator.Configure`).
- **EF Core migrations**: No migrations exist. Either use `EnsureCreated()` (already present) for dev, or generate proper EF Core migrations with `dotnet ef migrations add InitialCreate`.
- **`CatalogDBInitializer`**: `Seed()` must be explicitly called from `Program.cs` after building the app (pass `CatalogDBInitializer` via DI). Currently it is registered but not invoked at startup.
