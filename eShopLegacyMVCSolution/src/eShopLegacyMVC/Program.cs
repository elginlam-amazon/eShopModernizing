using Autofac;
using Autofac.Extensions.DependencyInjection;
using eShopLegacyMVC.Models;
using eShopLegacyMVC.Models.Infrastructure;
using eShopLegacyMVC.Modules;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace eShopLegacyMVC
{
    /// <summary>
    /// Application entry point. Replaces Global.asax (MvcApplication) and the App_Start/*Config classes.
    /// </summary>
    public class Program
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var useMockData = builder.Configuration.GetValue<bool>("UseMockData");

            // Dependency injection: keep Autofac and the existing ApplicationModule (was RegisterContainer()).
            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Host.ConfigureContainer<ContainerBuilder>(container =>
                container.RegisterModule(new ApplicationModule(useMockData)));

            builder.Services.AddDbContext<CatalogDBContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDBContext")));

            // MVC + Web API controllers. Keep PascalCase JSON property names, as produced by
            // Web API 2 (Json.NET) and MVC 5 Json() (JavaScriptSerializer).
            builder.Services.AddControllersWithViews()
                .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

            // <sessionState mode="InProc" />
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            var app = builder.Build();

            // <globalization uiCulture="en-US" culture="en-US" />
            var culture = new CultureInfo("en-US");
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(culture),
                SupportedCultures = new[] { culture },
                SupportedUICultures = new[] { culture }
            });

            // Application_BeginRequest: log4net per-request context properties.
            app.Use(async (context, next) =>
            {
                LogicalThreadContext.Properties["activityid"] = new ActivityIdHelper();
                LogicalThreadContext.Properties["requestinfo"] = new WebRequestInfo(context);

                _log.Debug("WebApplication_BeginRequest");

                await next();
            });

            // FilterConfig: HandleErrorAttribute rendered Views/Shared/Error.cshtml for unhandled exceptions.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();

            // Session_Start: track the machine name and the start time for the session inside the current session.
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

            // RouteConfig + WebApiConfig: attribute routes are discovered automatically.
            app.MapControllerRoute(
                name: "Default",
                pattern: "{controller=Catalog}/{action=Index}/{id?}");

            ConfigDataBase(app, useMockData);

            app.Run();
        }

        private static void ConfigDataBase(WebApplication app, bool useMockData)
        {
            if (!useMockData)
            {
                // Replaces Database.SetInitializer<CatalogDBContext>(CatalogDBInitializer) (CreateDatabaseIfNotExists).
                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<CatalogDBContext>();
                scope.ServiceProvider.GetRequiredService<CatalogDBInitializer>().InitializeDatabase(context);
            }
        }
    }

    public class ActivityIdHelper
    {
        public override string ToString()
        {
            if (Trace.CorrelationManager.ActivityId == Guid.Empty)
            {
                Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            }

            return Trace.CorrelationManager.ActivityId.ToString();
        }
    }

    public class WebRequestInfo
    {
        private readonly string requestInfo;

        // Captured when the request starts (was read lazily from HttpContext.Current), so the value
        // stays valid even if the logging event is rendered after the HttpContext has been recycled.
        public WebRequestInfo(HttpContext httpContext)
        {
            requestInfo = httpContext?.Request?.GetEncodedPathAndQuery() + ", " + httpContext?.Request?.Headers.UserAgent;
        }

        public override string ToString()
        {
            return requestInfo;
        }
    }
}
