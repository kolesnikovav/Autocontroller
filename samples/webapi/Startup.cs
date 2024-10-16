using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using AutoController;
using System.Text.Json.Serialization;

namespace webapi;

public class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;
    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<ApplicationDBContext>(options =>
            options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));
        services.AddAutoController<ApplicationDBContext>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
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
            WriteIndented = true,
        };
        var options = new AutoControllerOptions() {
            IsDevelopment = env.IsDevelopment(),
            JsonSerializerOptions = JsonOptions,
            LogInformation = true,
            RoutePrefix="api",
            AuthentificationPath  = "/login",
            AccessDeniedPath = "/anauthorized"
        };
        var optionsXML = new AutoControllerOptions() {
            InteractingType = InteractingType.XML,
            LogInformation = true,
            RoutePrefix="api2",
            AuthentificationPath  = "/login",
            AccessDeniedPath = "/anauthorized"
        };        

        app.UseAutoController<ApplicationDBContext>(options);
        app.UseAutoController<ApplicationDBContext>(optionsXML);
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

        app.UseSwagger();
        app.UseSwaggerUI();
    }
}

