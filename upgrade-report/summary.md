# 📊 Executive Report: eShopLegacyMVC Solution Upgrade to .NET 10  
  
## 1. 🧭 Executive Summary  
- 🎯 **Scope:** Upgraded 3 projects in `eShopLegacyMVCSolution` from .NET Framework / ASP.NET Core 2.2 to **.NET 10 (`net10.0`)**:  
  - `eShopLegacy.Utilities`  
  - `eShopPorted`  
  - `src/eShopLegacyMVC`  
- ⏱️ **Run window:** started 04:36:40 UTC on 2026-10-14. The Kiro CLI session took **11m 38s** (35.20 credits).  
- 🤖 **Approach:** Microsoft Upgrade Assistant ran first to convert project files. Kiro CLI then finished the code migration, fixed the build and hardened the result.  
- ✅ **Outcome:**  
  - `dotnet build` passes with **0 errors and 0 warnings** on all 3 projects.  
  - `dotnet list package --vulnerable` reports **no vulnerable packages**.  
  - `dotnet list package --deprecated` reports **no deprecated packages**.  
- 🏗️ **Main result:** the ASP.NET MVC 5 + Web API 2 + EF6 app (`eShopLegacyMVC`) was fully re-platformed to ASP.NET Core MVC + EF Core 10. Upgrade Assistant could not process this project on Linux, so Kiro CLI did the whole conversion.  
- ⚠️ **Not verified:** there are no test projects, and the app was not run. Validation covers compilation and publish layout only.  
  
## 2. 🧱 Application Changes  
| Project | Before | After | Upgrade Assistant result |  
|---|---|---|---|  
| 📦 eShopLegacy.Utilities | .NET Framework 4.6.1, classic csproj | SDK-style, multi-targeted `net10.0;netstandard2.0` | ✅ 10 steps succeeded, 1 skipped (converted to netstandard2.0 only) |  
| 🌐 eShopPorted | ASP.NET Core 2.2 on `net461` | ASP.NET Core on `net10.0` | ✅ 12 steps succeeded, 137 skipped |  
| 🛒 src/eShopLegacyMVC | ASP.NET MVC 5 + Web API 2 + EF6 on `net472` | ASP.NET Core MVC + EF Core 10 on `net10.0` | ❌ Failed: `InvalidProjectFileException` (the Windows-only `Microsoft.WebApplication.targets` is missing on Linux) |  
  
- 🔄 **Hosting:** `Global.asax` and `App_Start` (Route, WebApi, Filter and Bundle config) were replaced by a minimal-hosting `Program.cs`.  
- ⚙️ **Configuration:** settings moved from `Web.config` to `appsettings.json` and `appsettings.Development.json`, so each environment can override them.  
- 🗄️ **Data access:** moved from EF6 to EF Core 10 (SQL Server). Fluent mappings, HiLo id sequences and the "create database if it doesn't exist" seeding were all kept.  
- 🔌 **APIs:** Web API 2 `ApiController`s became ASP.NET Core `[ApiController]`s. URLs and PascalCase JSON output are unchanged.  
- 🎨 **UI and static files:** assets moved to `wwwroot`, bundles were replaced by direct tags with `asp-append-version`, and Razor views were updated.  
- 🧹 **Removed legacy items:**  
  - `Web*.config` and `Views/Web.config`  
  - `packages.config`  
  - `ApplicationInsights.config` (no instrumentation key was configured)  
  - Unused packages: WebGrease, Antlr, Autofac.Mvc5, Newtonsoft.Json and Microsoft.CSharp  
  
## 3. 🛠️ Tools Used  
- 🧰 **.NET SDK 10.0.401** (SDKs 5.0–8.0 are also installed): used for builds, restore, vulnerability and deprecation audits, and a publish layout check.  
- 🔧 **Microsoft .NET Upgrade Assistant 1.0.518.26217:** ran non-interactive `framework.inplace` upgrades for the SDK-style conversion and package upgrades.  
- 🤖 **Kiro CLI 2.14.2** with model **claude-opus-5.5**:  
  - Migrated the code and fixed the build.  
  - Mapped the old code with CodeKB before changing it.  
  - Used the migration knowledge base (docs 00, 01, 09, 16, 27 and 28).  
- 📚 **NuGet / GitHub Advisory data:** used to choose package versions and to fix a transitive security advisory.  
  
## 4. 💻 Code Changes  
- 🔐 **Serialization (Utilities)**  
  - 🚫 `BinaryFormatter` was removed in .NET 9+, so the `net10.0` build now uses `System.Text.Json`.  
  - 🛡️ The `netstandard2.0` build keeps `BinaryFormatter` (inside `#if NETSTANDARD2_0`), so .NET Framework callers behave as before.  
- 🚀 **Startup (eShopLegacyMVC `Program.cs`)**  
  - 🧩 Autofac is wired in through `AutofacServiceProviderFactory`, and the existing `ApplicationModule` is reused.  
  - 🍪 `Session_Start` became session middleware, and the layout still shows MachineName and SessionStartTime.  
  - 📝 `Application_BeginRequest` became middleware that sets log4net's `LogicalThreadContext` properties (`ActivityIdHelper`, `WebRequestInfo`).  
  - 🌍 The `en-US` culture is set through `UseRequestLocalization`.  
  - ❗ `HandleErrorAttribute` was replaced by `UseExceptionHandler("/Error")` plus a new `ErrorController`.  
- 🗄️ **EF Core (`CatalogDBContext`, `CatalogDBInitializer`, `CatalogItemHiLoGenerator`)**  
  - The context now takes `DbContextOptions<CatalogDBContext>`, and `HasRequired` became `HasOne().IsRequired()`.  
  - Price is mapped as `HasPrecision(18,2)`. Brand, type and item ids are `ValueGeneratedNever`, because the HiLo sequences assign them.  
  - `SqlQuery`/`ExecuteSqlCommand` became `SqlQueryRaw`/`ExecuteSqlRaw`, with `AsEnumerable()` so EF does not wrap `NEXT VALUE FOR` in a subquery.  
  - `HostingEnvironment` and `ConfigurationManager` were replaced by injected `IWebHostEnvironment` and `IConfiguration`. Script paths now work on Linux.  
- 🎮 **Controllers**  
  - `HttpStatusCodeResult`/`HttpNotFound` became `BadRequest()`/`NotFound()`, and `[Bind(Include=...)]` became `[Bind(...)]`.  
  - `Request.Url.Scheme` became `Request.Scheme`, and `Server.MapPath` became `ContentRootPath`.  
  - 🛡️ Path-traversal fix in `PicController`: it now uses `Path.GetFileName`.  
- 🌐 **eShopPorted**  
  - Its startup returned `IServiceProvider` and used `UseMvc`, both of which fail at runtime on modern ASP.NET Core. It now uses `Host`, Autofac `ConfigureContainer` and endpoint routing.  
  - `PicController` was moved from `System.Web.Mvc` to ASP.NET Core.  
- 🖼️ **Views**  
  - Added `_ViewImports.cshtml` to enable tag helpers.  
  - `Html.Partial` became `PartialAsync`, which clears the MVC1000 deadlock warning.  
  - `Styles.Render`/`Scripts.Render` were replaced by static tags.  
- 📦 **Packages:**  
  - Autofac 9.3.4  
  - Autofac.Extensions.DependencyInjection 11.0.2  
  - log4net 3.5.0  
  - EF Core 10.0.13  
  - 🔒 `System.Security.Cryptography.Pkcs` pinned to 10.0.13 (fixes GHSA-846f-f764-76mx and GHSA-2fmv-rv2c-59x7)  
- 🐧 **Linux fixes:**  
  - `log4Net.xml` is now referenced with its exact file-name case, and the log path uses `/`.  
  - The `images` folder name now matches the case used in the CSS and layout.  
- 📈 **Diff size:** 126 tracked files changed (+229 / −1,426 lines, mostly csproj cleanup and file moves). 7 new source and config files were added.  
  
## 5. ⏳ Time Savings Estimate  
| Work area | Manual estimate | Automated |  
|---|---|---|  
| 📄 Project file and SDK conversion (3 projects) | 2–3 h | ✅ |  
| 🔄 System.Web / MVC 5 / Web API 2 → ASP.NET Core | 6–10 h | ✅ |  
| 🗄️ EF6 → EF Core (mappings, initializer, raw SQL) | 4–6 h | ✅ |  
| ⚙️ Global.asax / Web.config → Program.cs / appsettings | 2–4 h | ✅ |  
| 🎨 Static assets, bundling, views | 2–3 h | ✅ |  
| 🔐 BinaryFormatter, security advisories, Linux fixes | 2–4 h | ✅ |  
| 📝 Build verification and documentation | 1–2 h | ✅ |  
| **Total** | **≈ 19–32 engineer-hours (≈ 3–4 days)** | **≈ 12 minutes of Kiro CLI time** |  
  
- 💡 **Estimated saving:** about **95%+** of hands-on migration effort, before human review and runtime testing.  
  
## 6. 🚀 Next Steps  
- 🧪 **Validation**  
  - ▶️ Run the app against SQL Server and smoke-test the main features:  
    - Catalog list, paging and CRUD  
    - The picture endpoint  
    - `api/brands`, `api/brands/{id}` and `api/files`  
    - Session info in the footer  
    - The error page outside Development  
  - 🗄️ Check that a fresh database is created and seeded correctly: HiLo sequences, and Type/Brand ids that are no longer IDENTITY columns. Also check that an existing EF6-created database works unchanged.  
  - 🧾 Add test projects (xUnit plus `WebApplicationFactory`) covering the controllers and `CatalogService`.  
- ⚠️ **Decisions needed**  
  - 🔄 `api/files` now returns JSON instead of a BinaryFormatter payload. Confirm no client depends on the old format.  
  - 🚦 The database is initialised at startup, so the app fails fast if SQL Server is unreachable (it used to fail on the first request). Confirm this is acceptable.  
  - 🐧 LocalDB only works on Windows. For Linux and containers, set `ConnectionStrings__CatalogDBContext` per environment.  
- 🛠️ **Pre-existing issues worth fixing**  
  - The SQL sequence scripts hard-code `USE [Microsoft.eShopOnContainers.Services.CatalogDb]`.  
  - The log4net pattern uses `%property{activity}`, but the code sets `activityid`.  
  - eShopPorted: `/Home/Error` and the `/Pics/{id}.png` links point to things that don't exist.  
- ✨ **Improvements**  
  - 📝 Move log4net to `Microsoft.Extensions.Logging` (for example Serilog or OpenTelemetry).  
  - 🧩 Consider the built-in DI container instead of Autofac.  
  - 📡 Re-introduce telemetry with `Microsoft.ApplicationInsights.AspNetCore` or OpenTelemetry if needed.  
  - 🐳 Add a Dockerfile (`mcr.microsoft.com/dotnet/aspnet:10.0`) and CI with build, test and vulnerability audit.  
  - 🔁 Consider merging or retiring the duplicate `eShopPorted` app now that `eShopLegacyMVC` runs on .NET 10.  

## Kiro credit usage

💳 Kiro CLI credits used: 79.05  

## Codebase index refresh

- Outcome: Index current
- Reason: Index current at 63bc9ec4414d7e281dc9f9d7bdcf70030950457d.
- Previously indexed commit: 63bc9ec4414d7e281dc9f9d7bdcf70030950457d
- Source commit at transformation start: 63bc9ec4414d7e281dc9f9d7bdcf70030950457d
- Duration: 0.0s

## Requirements register refresh

- Outcome: Timed out
- Reason: Register refresh exceeded 300000ms.
- Duration: 300.1s

> The requirements register was not refreshed for this transformation, so feature parity was checked against the **previous** register content. The transformation proceeded regardless.

## Feature Parity Verification

> **Parity results missing.** The feature-parity check did not produce a report, so parity could not be verified. Transformation sign-off is blocked.
>
> Detail: Parity re-derivation was aborted (300s refresh deadline) before the upgraded analysis was obtained.
