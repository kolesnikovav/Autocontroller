using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using DbContextExample;
using AutoController;

var builder = WebApplication.CreateBuilder(args);
var dbName = Guid.NewGuid().ToString();
builder.Services.AddDbContext<AppDBContext>(opt => opt.UseInMemoryDatabase(databaseName: dbName), ServiceLifetime.Scoped, ServiceLifetime.Scoped);
builder.Services.AddAutoController<AppDBContext>(DatabaseTypes.InMemory, dbName);

var app = builder.Build();

var JsonOptions = new JsonSerializerOptions
{
    WriteIndented = true
};
app.UseAutoController<AppDBContext>("api", true, InteractingType.JSON, "/login", "/anauthorized", JsonOptions);
app.UseAutoController<AppDBContext>("api2", true, InteractingType.XML, "/login", "/anauthorized");
app.UseAutoController<AppDBContext>("api3", true, null, "/login", "/anauthorized");

app.MapGet("/", () => "Hello World!");

app.Run();
