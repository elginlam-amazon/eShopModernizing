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
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;

// Replaces Global.asax / App_Start (RouteConfig, WebApiConfig, FilterConfig, BundleConfig).
var log = LogManager.GetLogger(typeof(Program));

var builder = WebApplication.CreateBuilder(args);

var useMockData = builder.Configuration.GetValue<bool>("UseMockData");

// Autofac container (http://docs.autofac.org/en/latest/integration/aspnetcore.html)
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(container =>
    container.RegisterModule(new ApplicationModule(useMockData)));

builder.Services.AddControllersWithViews()
    // Web API 2 / MVC 5 serialized JSON with PascalCase property names; keep that wire format.
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
builder.Services.AddDbContext<CatalogDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDBContext")));

// In-process session state (was <sessionState mode="InProc" /> in Web.config)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

var app = builder.Build();

if (!useMockData)
{
    // Was Database.SetInitializer<CatalogDBContext>(CatalogDBInitializer)
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<CatalogDBContext>();
    scope.ServiceProvider.GetRequiredService<CatalogDBInitializer>().InitializeDatabase(context);
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // Was the global HandleErrorAttribute filter rendering Views/Shared/Error.cshtml
    app.UseExceptionHandler("/Error");
}

// Was Application_BeginRequest
app.Use(async (httpContext, next) =>
{
    LogicalThreadContext.Properties["activityid"] = new ActivityIdHelper();
    LogicalThreadContext.Properties["requestinfo"] = new WebRequestInfo(httpContext.Request);
    log.Debug("WebApplication_BeginRequest");
    await next();
});

// Was <globalization uiCulture="en-US" culture="en-US" /> in Web.config
var culture = new CultureInfo("en-US");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = new[] { culture },
    SupportedUICultures = new[] { culture },
});

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Was Session_Start: track the machine name and the start time inside the current session
app.Use(async (httpContext, next) =>
{
    var session = httpContext.Session;
    if (session.GetString(SessionKeys.MachineName) == null)
    {
        session.SetString(SessionKeys.MachineName, Environment.MachineName);
        session.SetString(SessionKeys.SessionStartTime, DateTime.Now.ToString(CultureInfo.CurrentCulture));
    }
    await next();
});

app.MapControllers();
app.MapControllerRoute(
    name: "Default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

app.Run();
