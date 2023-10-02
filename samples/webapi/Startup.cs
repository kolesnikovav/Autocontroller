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
using Swashbuckle.AspNetCore.Annotations;
using AutoController;

namespace webapi;

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
        services.AddDbContext<ApplicationDBContext>(options =>
            options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));
        services.AddAutoController<ApplicationDBContext>(DatabaseTypes.SQLite, Configuration.GetConnectionString("DefaultConnection"));
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c => c.EnableAnnotations());
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
        app.UseRouting();
        app.UseAutoController<ApplicationDBContext>("api", true, InteractingType.JSON, "/login", "/anauthorized", JsonOptions);
        app.UseAutoController<ApplicationDBContext>("api2", true, InteractingType.XML, "/login", "/anauthorized");
        app.UseAutoController<ApplicationDBContext>("api3", true, null, "/login", "/anauthorized");

        

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Hello World!");
            }).WithMetadata(new SwaggerOperationAttribute("summary001", "description001"));
            //.WithDisplayName("default page").WithDescription("default page").WithName("default page").WithOpenApi();
            endpoints.MapGet("/login", async context =>
            {
                await context.Response.WriteAsync("Login page");
            }).WithDisplayName("login page").WithDescription("Login page").WithName("login").WithOpenApi();
            endpoints.MapGet("/anauthorized", async context =>
            {
                await context.Response.WriteAsync("Access Denied page");
            }).WithDisplayName("anauthorized page").WithDescription("Access Denied page").WithName("anauthorized").WithOpenApi();
        });

        app.UseSwagger();
        app.UseSwaggerUI();
    }
}

