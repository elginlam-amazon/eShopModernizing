using Autofac;
using Autofac.Extensions.DependencyInjection;
using eShopLegacyMVC;
using eShopLegacyMVC.Models;
using eShopLegacyMVC.Models.Infrastructure;
using eShopLegacyMVC.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;

// Replaces Global.asax (Application_Start, Application_BeginRequest, Session_Start) and App_Start/*.

var builder = WebApplication.CreateBuilder(args);

// log4net remains the log sink (log4Net.xml, rolling file appender); application code logs through ILogger<T>.
// Replaces [assembly: log4net.Config.XmlConfigurator(...)].
builder.Logging.AddLog4Net("log4Net.xml");

var useMockData = builder.Configuration.GetValue<bool>("UseMockData");

builder.Services
    .AddControllersWithViews()
    // MVC 5 JsonResult and Web API 2 (Json.NET) emitted PascalCase property names; keep that wire format.
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddDbContext<CatalogDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDBContext")));

// In-process session state (web.config <sessionState mode="InProc" />)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// Autofac container (http://docs.autofac.org/en/latest/integration/aspnetcore.html)
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
    containerBuilder.RegisterModule(new ApplicationModule(useMockData)));

var app = builder.Build();

if (!useMockData)
{
    // Equivalent of Database.SetInitializer(CatalogDBInitializer) (CreateDatabaseIfNotExists + Seed)
    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<CatalogDBInitializer>();
    initializer.InitializeDatabase(scope.ServiceProvider.GetRequiredService<CatalogDBContext>());
}

if (!app.Environment.IsDevelopment())
{
    // Replaces the global HandleErrorAttribute (FilterConfig)
    app.UseExceptionHandler("/Error");
}

// web.config <globalization uiCulture="en-US" culture="en-US" />
var appCulture = new CultureInfo("en-US");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(appCulture),
    SupportedCultures = new[] { appCulture },
    SupportedUICultures = new[] { appCulture }
});

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseMiddleware<SessionStartMiddleware>();
app.UseMiddleware<RequestLogContextMiddleware>();

// RouteConfig default route; attribute routes (PicController, API controllers) are mapped as well.
app.MapControllerRoute(
    name: "Default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

app.Run();
