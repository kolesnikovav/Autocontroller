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

- Pagination by default
- Sorting results
- Interacting type can be JSON or XML
- Flexible and adjustable request/response schema
- Filtering results
- Authentificated Access and Permitions Control
- Execute some Actions before save/delete
- Execute some Actions before DbContext SaveChanges/SaveChangesAsync

Routers and handlers for paths will be created:

- "/Your Entity/Index" GET
- "/Your Entity/Count" GET
- "/Your Entity/Save" POST
- "/Your Entity/Update" PUT
- "/Your Entity/Delete" DELETE

Interacting type can be JSON or XML.

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
            // you should add db context first!
            var connString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<ApplicationDBContext>(options =>
                options.UseSqlite(connString));
            // then add AddAutoController service
            services.AddAutoController<ApplicationDBContext>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // ---- Your code -----------//
            // Create your api
            // handle requests with default options
            app.UseAutoController<ApplicationDBContext>();
            // You can configure your api
            // RoutePrefix should be unique!
            var JsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var options = new AutoControllerOptions() {
                JsonSerializerOptions = JsonOptions,
                LogInformation = true,
                RoutePrefix="api2",
                AuthentificationPath  = "/login",
                AccessDeniedPath = "/anauthorized"
            };
            app.UseAutoController<ApplicationDBContext>(options);            
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
