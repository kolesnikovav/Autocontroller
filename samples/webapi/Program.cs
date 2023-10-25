using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.EntityFrameworkCore;
using AutoController;
using System.Text.Json;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connString = builder.Configuration.GetConnectionString("DefaultConnection");
var JsonOptions = new JsonSerializerOptions
{
    WriteIndented = true
};

builder.Services.AddDbContext<ApplicationDBContext>(options =>
            options.UseSqlite(connString));
builder.Services.AddAutoController<ApplicationDBContext>(
    DatabaseTypes.SQLite, connString!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.


app.UseHttpsRedirection();

app.UseAuthorization();



app.UseRouting();
app.UseAutoController<ApplicationDBContext>("api", true, InteractingType.JSON, "/login", "/anauthorized", JsonOptions);
app.UseAutoController<ApplicationDBContext>("api2", true, InteractingType.XML, "/login", "/anauthorized");
app.UseAutoController<ApplicationDBContext>("api3", true, null, "/login", "/anauthorized");
app.MapGet("/", async context => {
    await context.Response.WriteAsync("Hello World!");
})
.WithName("Index")
.WithMetadata(new SwaggerOperationAttribute("summary001", "description001"))
.WithDescription("Default page")
.WithOpenApi();
app.MapGet("/login", async context => {
    await context.Response.WriteAsync("Login page");
})
.WithName("Login")
.WithDescription("Login page")
.WithOpenApi();
app.MapGet("/anauthorized", async context => {
    await context.Response.WriteAsync("Access Denied page");
})
.WithName("Anauthorized")
.WithDescription("Access Denied page")
.WithOpenApi();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
var a = app.Services.GetRequiredService<IActionDescriptorCollectionProvider>();
foreach (var b in a.ActionDescriptors.Items)
{
    var rr = new ActionDescriptor();
    rr.DisplayName="fsfd";
    var c = b.EndpointMetadata;
}


//a.CreateControllerFactory()
app.Run();
