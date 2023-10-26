# Autocontroller
<a href="https://www.nuget.org/packages/AutoController">
    <img alt="Nuget (with prereleases)" src="https://img.shields.io/nuget/vpre/Autocontroller">
</a>
<a href="https://www.nuget.org/packages/AutoController">
    <img alt="Nuget" src="https://img.shields.io/nuget/dt/AutoController">
</a>

Automaticly create REST API endpoints from your Entity Framework Core database context.
CRUD actions becomes more easy!

## Supported features:

<ul>
<li>Pagination by default</li>
<li>Sorting results</li>
<li>Interacting type can be JSON or XML</li>
<li>Flexible and adjustable request/response schema</li>
<li>Filtering results</li>
<li>Authentificated Access and Permitions Control</li>
<li>Execute some Actions before save/delete</li>
<li>Execute some Actions before DbContext SaveChanges/SaveChangesAsync</li>
</ul>

Routers and handlers for paths will be created:
<ul>
<li>"/Your Entity/Index" GET</li>
<li>"/Your Entity/Count" GET</li>
<li>"/Your Entity/Save" POST</li>
<li>"/Your Entity/Update" PUT</li>
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
    "Blogs")]
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
            // you should describe database type and connection string here!
            var connString = Configuration.GetConnectionString("DefaultConnection");
            services.AddAutoController<ApplicationDBContext>(DatabaseTypes.SQLite, connString);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // ---- Your code -----------//
            // Create JsonSerializerOptions object if you use JSON interacting method
            var JsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            // Create your api
            // handle requests for api/<your entity>/... paths with Json
            app.UseAutoController<ApplicationDBContext>("api", true, InteractingType.JSON, JsonOptions);
            // handle requests for apixml/<your entity>/... paths with XML
            app.UseAutoController<ApplicationDBContext>("apixml", true, InteractingType.XML);
            // handle requests for apijson/<your entity>/... paths with Json
            app.UseAutoController<ApplicationDBContext>("apijson", true,  null, JsonOptions);

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

PUT Requests
https://localhost:5001/api/Blogs/Update - Update Blog into Your Database. Blog will be recived from request body

DELETE Requests
https://localhost:5001/api/Blogs/Delete - Remove Blog from Your Database. Blog will be recived from request body

## Handle some actions before save,  and delete object:

When You need do something before save and delete objects, You can implement interfaces
The method DoBeforeSave has been called before save object.
It returns true when object can be saved and deleted or false, when not.
Save and update requests and restrictions works the same.
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

    /// <summary>
    /// Implement this interface in your entity types
    /// </summary>
    public interface IActionBeforeDelete<T> where T:DbContext
    {
        /// <summary>
        /// Do something before delete
        /// </summary>
        /// <param name="dbcontext">DbContext</param>
        /// <param name="reason">The reason why the object cannot be removed</param>
        public bool DoBeforeDelete(T dbcontext, out string reason);
    }
```
