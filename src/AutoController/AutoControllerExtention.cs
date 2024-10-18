using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Http;

namespace AutoController;
/// <summary>
/// IApplicationBuilder extention for creating Autocotrollers
///
/// </summary>
public static class AutoControllerExtention
{
    private const string LogCategoryName = "AutoController";
    /// <summary>
    /// Adds AutoController as singletone service and register it in Dependency Injection
    ///
    /// </summary>
    /// <typeparam name="T">The type of your DBContext/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    public static void AddAutoController<T>(this IServiceCollection services) where T : DbContext
    {
        AutoRouterService<T> instance = new();
        
        IServiceProvider serviceProvider =  services.BuildServiceProvider();
        using var ctx = serviceProvider.GetRequiredService<T>();
        foreach (Type entityType in  ctx.Model.GetEntityTypes().Select(t => t.ClrType).ToList())
        {
            var eType = ctx.Model.FindEntityType(entityType);
            var keys = eType?.FindPrimaryKey()?.Properties.ToList() ?? [];
            if (keys.Count > 0)
            {
                List<EntityKeyDescribtion> entityKeyDescribtions = [];
                foreach (var key in keys)
                {
                    entityKeyDescribtions.Add(new EntityKeyDescribtion() { Name = key.Name, KeyType = key.ClrType});
                }
                AutoRouterService<T>.AddEntityKey(entityType,entityKeyDescribtions);
                AutoRouterService<T>.ProcessType(entityType);
            }
        }
        services.AddSingleton(typeof(AutoRouterService<T>), instance);
    }
    /// <summary>
    /// Retrives  AutoRouterService service for external use.
    ///
    /// </summary>
    /// <typeparam name="T">The type of your DBContext/>.</typeparam>
    /// <param name="builder">The applicaton builder</param>
    public static AutoRouterService<T> GetAutoRouterService<T>(IApplicationBuilder builder) where T : DbContext
    {
        var service = (AutoRouterService<T>?)builder.ApplicationServices.GetService(typeof(AutoRouterService<T>)) ?? throw (new Exception("You forgive register AutoRouterService in DI.\n Put services.AddAutoController<ApplicationDBContext>(); in ConfigureServices(IServiceCollection services) in Startup class"));
        return service;
    }
    private static void AddRoute(IApplicationBuilder builder, RouteKey key, RouteParameters parameters, IAutoControllerOptions options)
    {
        // var routeHandler = new RouteHandler(parameters.Handler);
        // var routeBuilder = new RouteBuilder(builder, routeHandler);
        // routeBuilder.MapRoute(key.ToString(), key.Path);
        // builder.UseRouter(routeBuilder.Build());
        builder.UseEndpoints(e => 
        {
            var a =e.MapAutoRoute(key,parameters.Handler, options);
        });        
    }
    private static IEndpointConventionBuilder MapAutoRoute(
        this IEndpointRouteBuilder endpointRouteBuilder,
        RouteKey key,
        RequestDelegate handler,
        IAutoControllerOptions options) 
    {
        IEndpointConventionBuilder result;
        string verb;
        if (key.HttpMethod == HttpMethod.Get)
        {
            verb = "GET";
           result =  endpointRouteBuilder.MapGet(key.Path,handler); 
        }
        else if (key.HttpMethod == HttpMethod.Post)
        {
            verb = "POST";
            result =  endpointRouteBuilder.MapPost(key.Path,handler);
        }
        else if (key.HttpMethod == HttpMethod.Patch)
        {
            verb = "PATCH";
            result = endpointRouteBuilder.MapPatch(key.Path,handler);
        }
        else if (key.HttpMethod == HttpMethod.Delete)
        {
            verb = "DELETE";
            result = endpointRouteBuilder.MapDelete(key.Path,handler);
        }
        else if (key.HttpMethod == HttpMethod.Put)
        {
            verb = "PUT";
            result = endpointRouteBuilder.MapPut(key.Path,handler);
        } 
        else
        {
            verb = "GET";
            result =  endpointRouteBuilder.MapGet(key.Path,handler);
        }
        var metadata = new AutoControllerRouteMetadata(verb,options.RoutePrefix,key.Path, key.Path ,"" , options.InteractingType);
        return result
        .WithName(key.Path)
        .WithDescription(key.ToString())
        .WithMetadata(metadata)
        .WithGroupName(options.RoutePrefix);

    }    

    /// <summary>
    /// Adds autocontroller for DBContext.
    ///
    /// </summary>
    /// <typeparam name="T">The DBContext derived type</typeparam>
    /// <param name="appBuilder">The ApplicationBuilder instance</param>
    /// <param name="options">Options for autocontroller</param>
    public static void UseAutoController<T>(
        this IApplicationBuilder appBuilder,
        IAutoControllerOptions? options = null) where T : DbContext
    {
        ArgumentNullException.ThrowIfNull(appBuilder);
        var actualOptions = options ?? new AutoControllerOptions();
        var logger = GetOrCreateLogger(appBuilder, LogCategoryName);
        AutoRouterService<T> autoRouter = GetAutoRouterService<T>(appBuilder);
        if (actualOptions.LogInformation)
        {
            autoRouter.AttachToLogger(logger);
        }
        var serviceProvider = appBuilder.ApplicationServices.GetService<IServiceProvider>();
        ArgumentNullException.ThrowIfNull(serviceProvider);        
        autoRouter.GetAutoControllers(actualOptions, serviceProvider);
        foreach (var route in autoRouter.Autoroutes)
        {
            AddRoute(appBuilder, route.Key, route.Value, actualOptions);
        }        
    }
    private static ILogger GetOrCreateLogger(
        IApplicationBuilder appBuilder,
        string logCategoryName)
    {
        // If the DI system gives us a logger, use it. Otherwise, set up a default one
        var loggerFactory = appBuilder.ApplicationServices.GetService<ILoggerFactory>();
        var logger = loggerFactory != null
            ? loggerFactory.CreateLogger(logCategoryName)
            : NullLogger.Instance;
        return logger;
    }
}
