using Autofac;
using Autofac.Extensions.DependencyInjection;
using eShopLegacyMVC.Models;
using eShopLegacyMVC.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

bool useMockData = builder.Configuration.GetValue<bool>("UseMockData");

if (!useMockData)
{
    string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
    builder.Services.AddDbContext<CatalogDBContext>(options =>
        options.UseSqlServer(connectionString)
    );
}

builder.Services.AddControllersWithViews();

builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterModule(new ApplicationModule(useMockData));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.MapControllerRoute("default", "{controller=Catalog}/{action=Index}/{id?}");
app.MapControllers();

app.Run();
