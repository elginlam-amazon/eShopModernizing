using Autofac;
using Autofac.Extensions.DependencyInjection;
using eShopLegacyMVC;
using eShopLegacyMVC.Models;
using eShopLegacyMVC.Models.Infrastructure;
using eShopLegacyMVC.Modules;
using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;

// Replaces Global.asax / App_Start (Application_Start, Session_Start, Application_BeginRequest,
// RouteConfig, WebApiConfig, FilterConfig and BundleConfig).

// log4net configuration (previously [assembly: XmlConfigurator(ConfigFile = "log4net.xml")])
XmlConfigurator.Configure(
    LogManager.GetRepository(Assembly.GetEntryAssembly()),
    new FileInfo(Path.Combine(AppContext.BaseDirectory, "log4Net.xml")));
var log = LogManager.GetLogger(typeof(Program));

var builder = WebApplication.CreateBuilder(args);

var useMockData = builder.Configuration.GetValue<bool>("UseMockData");

// Autofac stays the DI container (previously AutofacDependencyResolver / AutofacWebApiDependencyResolver)
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(container =>
    container.RegisterModule(new ApplicationModule(useMockData)));

builder.Services
    .AddControllersWithViews()
    // Keep PascalCase JSON like MVC 5 / Web API 2 (Newtonsoft default) did.
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

// Previously <connectionStrings><add name="CatalogDBContext" .../></connectionStrings> in Web.config
builder.Services.AddDbContext<CatalogDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDBContext")));

// Previously <sessionState mode="InProc" />
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Previously Database.SetInitializer(CreateDatabaseIfNotExists) in Application_Start
if (!useMockData)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<CatalogDBContext>();
    scope.ServiceProvider.GetRequiredService<CatalogDBInitializer>().InitializeDatabase(context);
}

// Previously FilterConfig (HandleErrorAttribute renders Views/Shared/Error.cshtml for remote clients)
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

// Previously <globalization uiCulture="en-US" culture="en-US" /> in Web.config
var culture = new CultureInfo("en-US");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = new[] { culture },
    SupportedUICultures = new[] { culture }
});

app.UseStaticFiles();

app.UseSession();

var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
app.Use(async (context, next) =>
{
    // Previously Session_Start: track the machine name and the start time inside the current session
    if (context.Session.GetString("MachineName") == null)
    {
        context.Session.SetString("MachineName", Environment.MachineName);
        context.Session.SetString("SessionStartTime", DateTime.Now.ToString());
    }

    // Previously Application_BeginRequest
    LogicalThreadContext.Properties["activityid"] = new ActivityIdHelper();
    LogicalThreadContext.Properties["requestinfo"] = new WebRequestInfo(httpContextAccessor);
    log.Debug("WebApplication_BeginRequest");

    await next();
});

app.UseRouting();

// Previously RouteConfig (MapMvcAttributeRoutes + Default route) and WebApiConfig (attribute routes)
app.MapControllerRoute(
    name: "Default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

app.Run();
