# Autocontroller
Automaticly create REST API Controllers for given types.
This project is designed for use with Entity Framework Core.

Routers and handlers for paths will be created:
<ul>
<li>"/Your Entity/Index" GET</li>
<li>"/Your Entity/Count" GET</li>
<li>"/Your Entity/Save" POST</li>
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

