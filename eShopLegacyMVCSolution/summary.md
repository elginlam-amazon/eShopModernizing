# .NET Framework → net10.0 Migration Summary

## Projects Migrated

| Project | Before | After | Result |
|---|---|---|---|
| eShopLegacy.Utilities | .NET Framework 4.7.2 | netstandard2.0 | 0 errors, 0 warnings |
| eShopPorted | .NET Framework 4.7.2 | net10.0 | 0 errors, 0 warnings |
| eShopLegacyMVC | .NET Framework 4.7.2 | net10.0 | 0 errors, 0 warnings |

## Key Changes Per Project

### eShopLegacy.Utilities
- Converted to SDK-style `.csproj` targeting `netstandard2.0`
- Added `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` to suppress CS0579 from existing `AssemblyInfo.cs`

### eShopPorted
- Converted to SDK-style `.csproj` targeting `net10.0`
- Removed legacy packages: `Autofac.Mvc5`, `WebGrease`, `Antlr4`, `Microsoft.Web.Infrastructure`
- Added: `Autofac 8.1.0`, `Autofac.Extensions.DependencyInjection 10.0.0`, `Microsoft.EntityFrameworkCore 10.0.0`
- `Startup.cs` + `Program.cs` (old) → new minimal hosting `Program.cs` with `AutofacServiceProviderFactory`
- `ConfigureContainer` lambda updated to two-parameter form `(context, containerBuilder) => ...`
- `log4net` → `Microsoft.Extensions.Logging` (`ILogger<T>`)
- Created `AppInfo.cs` to replace removed `Startup.StartTime` reference in `_Layout.cshtml`
- `@Html.Partial(...)` → `<partial name="..." model="..." />` tag helper (eliminates MVC1000)
- `Server.MapPath` → `IWebHostEnvironment.ContentRootPath`

### eShopLegacyMVC
- Converted old-format `.csproj` to SDK-style targeting `net10.0`
- `Global.asax` + `Global.asax.cs` deleted; replaced with minimal hosting `Program.cs`
- `Web.config` → `appsettings.json`
- `App_Start/` files replaced with stubs (routing/filters now in `Program.cs`)
- `Views/Web.config` deleted; `Views/_ViewImports.cshtml` added with tag helper registration
- `System.Web.Mvc` → `Microsoft.AspNetCore.Mvc` across all MVC controllers
- `System.Web.Http.ApiController` → `ControllerBase` + `[ApiController]` across all API controllers
- `HttpResponseMessage` / `IHttpActionResult` → `IActionResult`
- EF6 (`System.Data.Entity`) → EF Core 10 (`Microsoft.EntityFrameworkCore`):
  - `CatalogDBContext`: `EntityTypeConfiguration<T>` → `EntityTypeBuilder<T>` in `OnModelCreating(ModelBuilder)`
  - `HasDatabaseGeneratedOption(None)` → `.ValueGeneratedNever()`
  - `HasRequired<T>().WithMany().HasForeignKey()` → `HasOne<T>().WithMany().HasForeignKey()`
  - `Database.SqlQuery<T>` → `Database.SqlQueryRaw<T>`
  - `Database.ExecuteSqlCommand` → `Database.ExecuteSqlRaw`
  - `CreateDatabaseIfNotExists<T>` → explicit `Seed()` method with `context.Database.EnsureCreated()`
- `CatalogDBInitializer` refactored: removed `HostingEnvironment.ApplicationPhysicalPath`, injected `IWebHostEnvironment`/`IConfiguration`
- `log4net` → `Microsoft.Extensions.Logging` (`ILogger<T>`) in all controllers
- `@Styles.Render(...)` / `@Scripts.Render(...)` (MVC bundling) → direct `<link>`/`<script>` HTML tags
- `HttpContext.Current.Session` usage removed from `_Layout.cshtml`
- `@Html.Partial(...)` → `<partial>` tag helper in `Index.cshtml`

## Suppressed Warnings

| Code | File | Reason |
|---|---|---|
| EF1002 | `CatalogDBInitializer.cs:271` | `SqlQueryRaw` with string interpolation to build a SQL sequence name. The `dBSequenceName` parameter is an internal constant derived from a compile-time value, never user input — SQL injection risk is zero. |

## Items Not Migrated (Out of Scope)

- **MVC Bundling/Minification**: Static assets are now linked directly. Production optimization (fingerprinting, minification) should be handled by a bundler such as WebOptimizer or the npm/webpack pipeline.
- **Session state** (`HttpContext.Current.Session`): The machine-name display from the layout was removed. If session state is needed, add `builder.Services.AddSession()` and appropriate middleware in `Program.cs`.
- **Database migrations**: EF Core migrations were not generated. Run `dotnet ef migrations add InitialCreate` and `dotnet ef database update` to establish the schema, or rely on `EnsureCreated()` in `CatalogDBInitializer.Seed()` for development.
- **Authentication/Authorization**: Not present in the original codebase; no changes needed.
- **Deployment configuration**: `Web.config` transforms, IIS settings, and publish profiles were not migrated. Use environment-specific `appsettings.{Environment}.json` files and the ASP.NET Core hosting model.
