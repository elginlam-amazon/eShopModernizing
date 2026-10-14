# eShopLegacyMVC solution – .NET Framework → net10.0

## Result
`dotnet build` succeeds with 0 errors and 0 warnings for all three projects. No vulnerable or deprecated packages remain.

| Project | Before | After |
|---|---|---|
| eShopLegacy.Utilities | net461, classic csproj | SDK-style, `net10.0;netstandard2.0` |
| eShopPorted | ASP.NET Core 2.2 on net461 | ASP.NET Core on net10.0 |
| src/eShopLegacyMVC | ASP.NET MVC 5 + Web API 2 + EF6 on net472 | ASP.NET Core MVC + EF Core 10 on net10.0 |

## Changes worth knowing
- **Utilities `Serializing`**: `BinaryFormatter` no longer exists in .NET 9+. On `net10.0` the class now writes and reads JSON with `System.Text.Json`, and `DeserializeBinary` returns a `JsonElement`. The `netstandard2.0` build still uses `BinaryFormatter`, so .NET Framework callers behave as before. As a result, `GET api/files` now returns JSON (`application/json`) instead of a BinaryFormatter blob.
- **eShopLegacyMVC hosting**: `Global.asax` and `App_Start` were replaced by `Program.cs`, which covers routing, the error page, bundles, Autofac setup, database initialisation, `Session_Start` (as session middleware) and `Application_BeginRequest` (setting log4net `LogicalThreadContext` properties). Autofac and log4net are kept, now through `AutofacServiceProviderFactory` and explicit `XmlConfigurator.Configure`.
- **Configuration**: settings moved from `Web.config` to `appsettings.json` (`ConnectionStrings:CatalogDBContext`, `UseMockData`, `UseCustomizationData`). The `en-US` culture is set through `UseRequestLocalization`.
- **EF6 → EF Core**: `CreateDatabaseIfNotExists` is now `CatalogDBInitializer.InitializeDatabase`, which runs at startup when `UseMockData=false`; seeding only runs if `EnsureCreated()` creates the database. Raw SQL calls now use `SqlQueryRaw`/`ExecuteSqlRaw`, with `AsEnumerable()` so EF does not wrap `NEXT VALUE FOR` in a subquery. Brand and type ids are now `ValueGeneratedNever`, because the initializer assigns them from the HiLo sequences (same values, but a new database gets no IDENTITY column). Price is `decimal(18,2)`.
- **Web API 2 → ASP.NET Core**: the API controllers are now `[ApiController]` with attribute routes on the same URLs (`api/brands`, `api/brands/{id}`, `api/files`). JSON keeps PascalCase property names, as before. A 404 now returns a ProblemDetails body.
- **Static assets**: `Content`, `Scripts` and `fonts` moved to `wwwroot`. `Images` moved to `wwwroot/images` (lower case, to match the existing references on case-sensitive Linux). Bundles were replaced with direct `<link>`/`<script>` tags using `asp-append-version`, since the assets are already prebuilt. `Pics` and `Setup` stay in the content root and are not publicly served.
- **Removed**: Application Insights (it had no instrumentation key), `Web*.config`, `packages.config`, `Views/Web.config`, and unused packages (WebGrease, Antlr, Newtonsoft.Json, Microsoft.CSharp, Autofac.Mvc5).
- **Small fixes**:
  - `PicController` uses `Path.GetFileName` so a crafted `PictureFileName` cannot be used for path traversal.
  - The log4net log path now uses `/`.
  - The `log4Net.xml` file name now matches its exact case.
  - `System.Security.Cryptography.Pkcs` is pinned to 10.0.13 to fix GHSA-846f-f764-76mx and GHSA-2fmv-rv2c-59x7.
- **eShopPorted**:
  - Startup previously returned `IServiceProvider` and called `UseMvc`, both of which fail at runtime on ASP.NET Core 3+. It now uses `Host` with `AutofacServiceProviderFactory`, `ConfigureContainer`, and endpoint routing.
  - `PicController` was moved from System.Web.Mvc to ASP.NET Core.

## Next steps
- The FilesController wire format changed from binary to JSON. Confirm no client depends on the BinaryFormatter payload.
- Database initialisation now runs at startup, so the app fails fast if SQL Server is unreachable. The old app failed on the first request instead.
- The default connection string uses LocalDB, which only works on Windows. On Linux or in containers, set `ConnectionStrings__CatalogDBContext`.
- The `.sql` sequence scripts contain `USE [Microsoft.eShopOnContainers.Services.CatalogDb]` (unchanged from the original). They break if the database name differs.
- `log4Net.xml` uses `%property{activity}`, but the code sets `activityid`. This mismatch was already there; fix it if activity ids should appear in the log.
- eShopPorted: `UseExceptionHandler("/Home/Error")` points to a controller that does not exist (this was already the case). Its `CatalogController` builds `/Pics/{id}.png` URLs that no static folder serves.
- Consider moving log4net to `Microsoft.Extensions.Logging` and Autofac to the built-in DI container in a later pass. Both were kept here to avoid changing the existing patterns.
- No test projects exist, so nothing beyond compilation and publish layout was verified. The app was not run.
