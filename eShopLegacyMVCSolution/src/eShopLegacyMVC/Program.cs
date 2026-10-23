using System;
using System.Globalization;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using eShopLegacyMVC.Models;
using eShopLegacyMVC.Models.Infrastructure;
using eShopLegacyMVC.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

var useMockData = builder.Configuration.GetValue<bool>("UseMockData");

builder.Services
    .AddControllersWithViews()
    // Keep the PascalCase JSON contract produced by the former Web API 2 (Newtonsoft) controllers
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddDbContext<CatalogDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDBContext")));

// Replaces <sessionState mode="InProc" />
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// Autofac container (replaces AutofacDependencyResolver / AutofacWebApiDependencyResolver)
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(container =>
    container.RegisterModule(new ApplicationModule(useMockData)));

var app = builder.Build();

// Replaces the CatalogDBInitializer registration (Database.SetInitializer) from Global.asax
if (!useMockData)
{
    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<CatalogDBInitializer>();
    initializer.InitializeDatabase(scope.ServiceProvider.GetRequiredService<CatalogDBContext>());
}

// Replaces HandleErrorAttribute (FilterConfig) / customErrors
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

// Replaces <globalization uiCulture="en-US" culture="en-US" />
var defaultCulture = new CultureInfo("en-US");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new[] { defaultCulture },
    SupportedUICultures = new[] { defaultCulture }
});

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Replaces Application_BeginRequest (log4net activity id / request info) and Session_Start
var requestLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("eShopLegacyMVC.Request");
app.Use(async (context, next) =>
{
    var requestInfo = $"{context.Request.Path}{context.Request.QueryString}, {context.Request.Headers.UserAgent}";
    using (requestLogger.BeginScope("ActivityId:{ActivityId} RequestInfo:{RequestInfo}", context.TraceIdentifier, requestInfo))
    {
        requestLogger.LogDebug("WebApplication_BeginRequest");

        // Track the machine name and the start time for the session inside the current session
        if (context.Session.GetString("MachineName") == null)
        {
            context.Session.SetString("MachineName", Environment.MachineName);
            context.Session.SetString("SessionStartTime", DateTime.Now.ToString(CultureInfo.CurrentCulture));
        }

        await next(context);
    }
});

// Replaces RouteConfig (MapMvcAttributeRoutes + Default route) and WebApiConfig (attribute routes)
app.MapControllers();
app.MapControllerRoute(
    name: "Default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

app.Run();
