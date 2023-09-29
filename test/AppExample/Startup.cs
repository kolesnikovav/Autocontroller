using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using AutoController;
using DbContextExample;

namespace AppExample;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }
    public IConfiguration Configuration { get; }
    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<DbContextExample.AppDBContext>(opt => opt.UseInMemoryDatabase(databaseName: dbName), ServiceLifetime.Scoped, ServiceLifetime.Scoped);
        services.AddAutoController<DbContextExample.AppDBContext>(DatabaseTypes.InMemory, dbName);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        var JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        app.UseAutoController<DbContextExample.AppDBContext>("api", true, InteractingType.JSON, "/login", "/anauthorized", JsonOptions);
        app.UseAutoController<DbContextExample.AppDBContext>("api2", true, InteractingType.XML, "/login", "/anauthorized");
        app.UseAutoController<DbContextExample.AppDBContext>("api3", true, null, "/login", "/anauthorized");

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
            endpoints.MapGet("/login", async context =>
            {
                await context.Response.WriteAsync("Login page");
            });
            endpoints.MapGet("/anauthorized", async context =>
            {
                await context.Response.WriteAsync("Access Denied page");
            });
        });
    }
}
