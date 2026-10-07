# Migration Summary: .NET Framework → .NET 10

## Result
All three projects build successfully with **0 errors, 0 warnings**.

---

## eShopLegacy.Utilities

**Changes:**
- Added `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` to suppress duplicate attribute errors from legacy `Properties/AssemblyInfo.cs`
- Added `<LangVersion>latest</LangVersion>` for C# 8+ syntax support
- Removed unused `Microsoft.CSharp` and `System.Data.DataSetExtensions` package references
- Added `System.Text.Json` package for `netstandard2.0` compatibility
- Replaced `BinaryFormatter` (SYSLIB0011 – removed in .NET 5+) in `Serializing.cs` with `System.Text.Json.JsonSerializer`. The public API (Stream-based) is preserved.

---

## eShopPorted

**Changes:**
- Removed Framework-only packages: `Autofac.Mvc5 4.0.2`, `WebGrease 1.6.0`, `Antlr 3.4.1` (Antlr was a WebGrease dependency), `Microsoft.CSharp`
- Upgraded `Autofac` → 9.3.4 and `Autofac.Extensions.DependencyInjection` → 11.0.2
- Upgraded `log4net` 2.0.10 → 3.5.0 (fixes NU1902 vulnerability advisory)
- Rewrote `Program.cs` using ASP.NET Core minimal hosting (`WebApplication.CreateBuilder`) with `AutofacServiceProviderFactory`, replacing the obsolete `WebHost.CreateDefaultBuilder` / `IWebHostBuilder` pattern
- Removed `Startup.cs` (functionality moved to `Program.cs`)
- Rewrote `Controllers/PicController.cs` to use `Microsoft.AspNetCore.Mvc` instead of `System.Web.Mvc`; injected `IWebHostEnvironment` to replace `Server.MapPath`
- Removed `System.Web` import from `Models/Infrastructure/PreconfiguredData.cs`
- Removed `eShopPorted.Startup.StartTime` reference from `Views/Shared/_Layout.cshtml`
- Fixed MVC1000 warning: changed `@Html.Partial` to `<partial>` tag helper in `Views/Catalog/Index.cshtml`

---

## eShopLegacyMVC

**Changes:**

### Project file
- Replaced old-format `ToolsVersion=15.0` project file with SDK-style `Microsoft.NET.Sdk.Web` targeting `net10.0`
- Added modern packages: `Autofac 9.3.4`, `Autofac.Extensions.DependencyInjection 11.0.2`, `log4net 3.5.0`, `Microsoft.EntityFrameworkCore 10.0.12`, `Microsoft.EntityFrameworkCore.SqlServer 10.0.12`, `Newtonsoft.Json 13.0.4`
- Set `<Nullable>disable</Nullable>` for legacy code compatibility
- Excluded legacy files from build: `Global.asax`, `Web.config`, `Views/Web.config`, `ApplicationInsights.config`

### Application startup
- Created `Program.cs` with ASP.NET Core minimal hosting, Autofac DI, and EF Core DB context registration
- Stubbed `Global.asax.cs` (functionality moved to `Program.cs`)
- Stubbed all `App_Start/` files: `BundleConfig.cs`, `FilterConfig.cs`, `RouteConfig.cs`, `WebApiConfig.cs`
- Created `appsettings.json` with connection string and mock data configuration
- Created `Views/_ViewImports.cshtml` with namespace imports and tag helper registration

### Controllers
- `Controllers/CatalogController.cs`: `System.Web.Mvc` → `Microsoft.AspNetCore.Mvc`; fixed `Bind(Include=...)` → `Bind("...")`, `HttpStatusCodeResult(BadRequest)` → `BadRequest()`, `HttpNotFound()` → `NotFound()`, `Request.Url.Scheme` → `Request.Scheme`
- `Controllers/PicController.cs`: Migrated to ASP.NET Core; injected `IWebHostEnvironment` to replace `Server.MapPath`
- `Controllers/Api/CatalogController.cs`: Updated namespace to `Microsoft.AspNetCore.Mvc`
- `Controllers/WebApi/BrandsController.cs`: `ApiController` → `ControllerBase`; removed `System.Runtime.Remoting.Messaging` and `System.Web.Http`; `IHttpActionResult` → `IActionResult`
- `Controllers/WebApi/FilesController.cs`: Same ApiController migration; `HttpResponseMessage` return → `IActionResult`; returns `File(stream, ...)`

### Models
- `Models/CatalogDBContext.cs`: EF6 → EF Core; `DbContext(string connectionString)` → `DbContext(DbContextOptions<T>)`; `DbModelBuilder`/`EntityTypeConfiguration<T>` → `ModelBuilder` with lambda configuration; `HasDatabaseGeneratedOption(None)` → `.ValueGeneratedNever()`; `HasRequired<T>` → `HasOne<T>`
- `Models/CatalogBrand.cs`, `CatalogType.cs`: Removed `System.Web` and other unused imports
- `Models/CatalogItemHiLoGenerator.cs`: Removed `System.Web`; `db.Database.SqlQuery<Int64>` → `db.Database.SqlQuery<long>($"...")` (EF Core 7+ scalar query API)
- `Models/Infrastructure/CatalogDBInitializer.cs`: Complete rewrite; removed EF6 `CreateDatabaseIfNotExists<>` base class; changed to standalone class with `Initialize(CatalogDBContext, contentRootPath, useCustomizationData)` method; `HostingEnvironment.ApplicationPhysicalPath` → injected `contentRootPath` parameter; `ConfigurationManager.AppSettings` → caller passes configuration; `context.Database.ExecuteSqlCommand` → `ExecuteSqlRaw`; `context.Database.SqlQuery<Int64>` → `SqlQuery<long>(FormattableString)`
- `Models/Infrastructure/PreconfiguredData.cs`: Removed `System.Web` import
- `Services/CatalogService.cs`: `System.Data.Entity` → `Microsoft.EntityFrameworkCore` for `EntityState`

### Modules
- `Modules/ApplicationModule.cs`: Removed `CatalogDBInitializer` and `CatalogDBContext` registrations (these are handled by ASP.NET Core DI and Program.cs); kept `CatalogItemHiLoGenerator` as singleton

### Views
- `Views/Shared/_Layout.cshtml`: Replaced `@Styles.Render` / `@Scripts.Render` with direct CDN `<link>` / `<script>` tags; replaced `HttpContext.Current.Session` with `@Environment.MachineName`
- `Views/Catalog/Create.cshtml`, `Edit.cshtml`: Replaced `@Scripts.Render("~/bundles/jqueryval")` with direct CDN script tag
- `Views/Catalog/Index.cshtml`: Replaced `@Html.Partial` with `<partial>` tag helper (fixes MVC1000 warning)

---

## Next steps

- **eShopLegacyMVC runtime verification**: The app has not been run — DB initialization with `CatalogDBInitializer.Initialize()` is called from `Program.cs` and depends on a SQL Server connection. With `UseMockData=true` (the default), DB initialization is skipped and the app should start fine.
- **EF Core migrations**: The migrated `eShopLegacyMVC` uses `EnsureCreated()` for DB initialization (not migrations). For production use, consider adding EF Core migrations.
- **log4net configuration**: The legacy `log4Net.xml` is present but not wired up in `Program.cs`. Add `XmlConfigurator.Configure()` call if log4net output is needed at runtime.
- **Serializing API change**: `Serializing.SerializeBinary` now produces JSON bytes (not binary). Any existing clients consuming the binary format from `FilesController` will need to be updated.
