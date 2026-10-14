using Autofac;
using Autofac.Extensions.DependencyInjection;
using eShopLegacyMVC;
using eShopLegacyMVC.Models;
using eShopLegacyMVC.Models.Infrastructure;
using eShopLegacyMVC.Modules;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;

// Replaces Global.asax (Application_Start, Session_Start, Application_BeginRequest) and App_Start/*.
var log = LogManager.GetLogger(typeof(Program));

var builder = WebApplication.CreateBuilder(args);

var useMockData = builder.Configuration.GetValue<bool>("UseMockData");

// Autofac stays the DI container; ApplicationModule keeps the service registrations.
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(container =>
    container.RegisterModule(new ApplicationModule(useMockData)));

// MVC + Web API controllers. PascalCase JSON keeps the contract of the old Web API (Json.NET) / MVC Json() output.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddDbContext<CatalogDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDBContext")));

// <sessionState mode="InProc" />
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ConfigDataBase(): EF6 CreateDatabaseIfNotExists initializer
if (!useMockData)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<CatalogDBContext>();
    scope.ServiceProvider.GetRequiredService<CatalogDBInitializer>().InitializeDatabase(context);
}

// Application_BeginRequest: per-request log4net context properties
app.Use(async (context, next) =>
{
    LogicalThreadContext.Properties["activityid"] = new ActivityIdHelper();
    LogicalThreadContext.Properties["requestinfo"] =
        new WebRequestInfo(context.RequestServices.GetRequiredService<IHttpContextAccessor>());

    log.Debug("WebApplication_BeginRequest");
    await next();
});

// FilterConfig: HandleErrorAttribute -> Views/Shared/Error.cshtml (developer exception page in Development)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

// <globalization uiCulture="en-US" culture="en-US" />
var culture = new CultureInfo("en-US");
app.UseRequestLocalization(options =>
{
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(culture);
    options.SupportedCultures = new[] { culture };
    options.SupportedUICultures = new[] { culture };
});

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Session_Start: track the machine name and the start time for the session inside the current session
app.Use(async (context, next) =>
{
    await context.Session.LoadAsync();
    if (context.Session.GetString("MachineName") == null)
    {
        context.Session.SetString("MachineName", Environment.MachineName);
        context.Session.SetString("SessionStartTime", DateTime.Now.ToString());
    }
    await next();
});

// RouteConfig + WebApiConfig (attribute routes are mapped as well)
app.MapControllerRoute(
    name: "Default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

app.Run();
