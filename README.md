# Autocontroller
<a href="https://www.nuget.org/packages/AutoController">
    <img alt="Nuget (with prereleases)" src="https://img.shields.io/nuget/vpre/Autocontroller">
</a>
<a href="https://www.nuget.org/packages/AutoController">
    <img alt="Nuget" src="https://img.shields.io/nuget/dt/AutoController">
</a>

Automaticly create REST API Controllers for given types.
This project is designed for use with Entity Framework Core.
CRUD actions becomes more easy!

## Supported features:

<ul>
<li>Pagination by default</li>
<li>Sorting results</li>
<li>Interacting type can be JSON or XML</li>
<li>Flexible and adjustable request/response schema</li>
<li>Filtering results</li>
<li>Authentificated Access and Permitions Control</li>
<li>(TODO) Execute some Actions before save/delete</li>
</ul>

Routers and handlers for paths will be created:
<ul>
<li>"/Your Entity/Index" GET</li>
<li>"/Your Entity/Count" GET</li>
<li>"/Your Entity/Save" POST</li>
<li>"/Your Entity/Delete" DELETE</li>
</ul>
Interacting type can be JSON or XML.

Database supported:
<ul>
<li>SQLLite</li>
<li>SQLServer</li>
<li>PostgreSQL</li>
</ul>

## How to use
Install package via nugget
```
dotnet add package AutoController
```
Mark your entity type as a member of AutoController by using Attribute
```cs
[MapToController(
    // name & adress of controller
    "Blogs",
    // interacting type
    InteractingType.JSON,
    // default pagesize
    25,
    // AllowAnonimus true by default
    false)]
```
Create some restrictions, if needed
These attributes inherits from Authorize attribute and woks the same.
It can be combined
```cs
// GET access restriction
[GetRestriction(Roles = "Administrator")]
// POST access restriction
// user must have Administrator role
[PostRestriction(Roles = "Administrator")]
// DELETE access restriction
[DeleteRestriction(Roles = "Administrator")]
```

Change your Startup.cs configuration file as follows:
```cs
using AutoController;
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // ---- Your code -----------//
            // Register Autocontroller in default DI container
            services.AddAutoController<ApplicationDBContext>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // ---- Your code -----------//
            // Get the database connection string
            string connStr = Configuration.GetConnectionString("DefaultConnection");
            // Create JsonSerializerOptions object if you use JSON interacting method
            var JsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            // Create your api
            // handle requests for api/<your entity>/... paths with Json
            app.UseAutoController<ApplicationDBContext>("api", true, DatabaseTypes.SQLite, connStr, InteractingType.JSON, JsonOptions);
            // handle requests for apixml/<your entity>/... paths with XML
            app.UseAutoController<ApplicationDBContext>("apixml", true, DatabaseTypes.SQLite, connStr, InteractingType.XML);
            // handle requests for apijson/<your entity>/... paths with Json
            app.UseAutoController<ApplicationDBContext>("apijson", true, DatabaseTypes.SQLite, connStr, null, JsonOptions);

        }
    }
```
When Your Application has been started, You can see these responses:

GET Requests
https://localhost:5001/api/Blogs/Index - JSON Result
https://localhost:5001/apixml/Blogs/Index - XML Result

https://localhost:5001/api/Blogs/Index?page=1&size=5 - JSON Result with first 5 items
https://localhost:5001/api/Blogs/Index?page=1&size=5&sort=Content - JSON Result with first 5 items, ordered by Content

Filtering results:
for filtering results, the library
https://dynamic-linq.net/
has been used.
https://localhost:5001/api/Blogs/Index?filter=id = 1 - JSON Result with item with id = 1
https://localhost:5001/api/Blogs/Index?filter=Content!=null and Subject !=null - JSON Result with items with fiter conditions



POST Requests
https://localhost:5001/api/Blogs/Save - Save Blog into Your Database. Blog will be recived from request body

DELETE Requests
https://localhost:5001/api/Blogs/Delete - Remove Blog from Your Database. Blog will be recived from request body

## Handle some actions before save object:

With your Entity type, where it's nessesary, You can implement interface
The method DoBeforeSave has been called before save object.
It returns true when object can be saved or false, when not.
It returns reason text with responce.
```cs
    /// <summary>
    /// Implement this interface in your entity types
    /// </summary>
    public interface IActionBeforeSave<T> where T:DbContext
    {
        /// <summary>
        /// Do something before save
        /// </summary>
        /// <param name="dbcontext">DbContext</param>
        /// <param name="reason">The reason why the object cannot be saved</param>
        public bool DoBeforeSave(T dbcontext, out string reason);
    }
```

