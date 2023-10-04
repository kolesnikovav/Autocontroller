using System;
using System.Linq;
using System.Text.Json;
using System.Net.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

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
    [Obsolete("Use version with database type and connection string")]
    public static void AddAutoController<T>(this IServiceCollection services) where T : DbContext
    {
        services.AddSingleton(typeof(AutoRouterService<T>));
    }
    /// <summary>
    /// Adds AutoController as singletone service and register it in Dependency Injection
    ///
    /// </summary>
    /// <typeparam name="T">The type of your DBContext/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="dbType">The type of Database</param>
    /// <param name="connString">Connection string</param>
    /// <param name="DbContextBeforeSaveChangesMethod">Method of DbContext to execute it before save data</param>
    /// <param name="DbContextFactory">Custom DbContextFactory</param>
    public static void AddAutoController<T>(this IServiceCollection services, DatabaseTypes dbType, string connString, MethodInfo DbContextBeforeSaveChangesMethod = null, Func<T> DbContextFactory = null) where T : DbContext
    {
        AutoRouterService<T>.SetStaticParams(dbType, connString, DbContextBeforeSaveChangesMethod, DbContextFactory);
        services.AddSingleton(typeof(AutoRouterService<T>));
    }
    /// <summary>
    /// Retrives  AutoRouterService service for external use.
    ///
    /// </summary>
    /// <typeparam name="T">The type of your DBContext/>.</typeparam>
    /// <param name="builder">The applicaton builder</param>
    public static AutoRouterService<T> GetAutoRouterService<T>(IApplicationBuilder builder) where T : DbContext
    {
        var service = builder.ApplicationServices.GetService(typeof(AutoRouterService<T>)) ?? throw (new Exception("You forgive register AutoRouterService in DI.\n Put services.AddAutoController<ApplicationDBContext>(); in ConfigureServices(IServiceCollection services) in Startup class"));
        return (AutoRouterService<T>)service;
    }
    private static IEndpointConventionBuilder MapAutoRoute(this IEndpointRouteBuilder endpointRouteBuilder, RouteKey key, RequestDelegate handler, string api_prefix) 
    {
        IEndpointConventionBuilder result;
        if (key.HttpMethod == HttpMethod.Get)
        {
           result =  endpointRouteBuilder.MapGet(key.Path,handler); 
        }
        else if (key.HttpMethod == HttpMethod.Post)
        {
            result =  endpointRouteBuilder.MapPost(key.Path,handler);
        }
        else if (key.HttpMethod == HttpMethod.Patch)
        {
            result = endpointRouteBuilder.MapPatch(key.Path,handler);
        }
        else if (key.HttpMethod == HttpMethod.Delete)
        {
            result = endpointRouteBuilder.MapDelete(key.Path,handler);
        }
        else if (key.HttpMethod == HttpMethod.Put)
        {
            result = endpointRouteBuilder.MapPut(key.Path,handler);
        } 
        else
        {
            result =  endpointRouteBuilder.MapGet(key.Path,handler);
        }
        Console.WriteLine(key.Path);
        return result.WithName(key.Path); 
    }    
    private static void AddRoute(IApplicationBuilder builder, string api_prefix, RouteKey key, RouteParameters parameters)
    {
        builder.UseEndpoints(e => 
        {
            e.MapAutoRoute(key,parameters.Handler, api_prefix);
        });
    }
    /// <summary>
    /// Adds autocontroller for DBContext.
    ///
    /// </summary>
    /// <typeparam name="T">The DBContext derived type</typeparam>
    /// <param name="appBuilder">The instance of ApplicationBuilder</param>
    /// <param name="routePrefix">start prefix for route</param>
    /// <param name="useLogging">log information for route</param>
    /// <param name="databaseType">The type of your database</param>
    /// <param name="connectionString">Database connection string</param>
    /// <param name="interactingType">Interacting type</param>
    /// <param name="authentificationPath">Application authentification path</param>
    /// <param name="accessDeniedPath">Application access denied page path</param>
    /// <param name="jsonOptions">Sets the JsonSerializerOptions</param>
    /// <param name="DefaultGetAction">The action path of default GET request. Default = Index</param>
    /// <param name="DefaultGetCountAction">The action path of default Count GET request. Default = Count</param>
    /// <param name="DefaultPostAction">The action path of default POST request. Default = Save</param>
    /// <param name="DefaultDeleteAction">The action path of default DELETE request. Default = Delete</param>
    /// <param name="DefaultFilterParameter">default = filter</param>
    /// <param name="DefaultSortParameter">default = sort</param>
    /// <param name="DefaultSortDirectionParameter">default = sortdirection</param>
    /// <param name="DefaultPageParameter">default = page</param>
    /// <param name="DefaultItemsPerPageParameter">default = size</param>
    /// <param name="DefaultUpdateAction">The action path of default PUT request. Default = Update</param>
    [Obsolete("Use version without database type and connection string")]
    public static void UseAutoController<T>(
        this IApplicationBuilder appBuilder,
        string routePrefix,
        bool useLogging,
        DatabaseTypes databaseType,
        string connectionString,
        InteractingType? interactingType,
        string authentificationPath,
        string accessDeniedPath,
        JsonSerializerOptions? jsonOptions = null,
        string DefaultGetAction = "Index",
        string DefaultGetCountAction = "Count",
        string DefaultPostAction = "Save",
        string DefaultDeleteAction = "Delete",
        string DefaultFilterParameter = "filter",
        string DefaultSortParameter = "sort",
        string DefaultSortDirectionParameter = "sortdirection",
        string DefaultPageParameter = "page",
        string DefaultItemsPerPageParameter = "size",
        string DefaultUpdateAction = "Update"
        ) where T : DbContext
    {
        if (appBuilder == null)
        {
            throw new ArgumentNullException(nameof(appBuilder));
        }
        var logger = GetOrCreateLogger(appBuilder, LogCategoryName);
        AutoRouterService<T> autoRouter = GetAutoRouterService<T>(appBuilder);
        if (useLogging)
        {
            autoRouter.AttachToLogger(logger);
        }
        autoRouter.GetAutoControllers(
            routePrefix,
            databaseType,
            connectionString,
            interactingType,
            authentificationPath,
            accessDeniedPath,
            jsonOptions,
            DefaultGetAction,
            DefaultGetCountAction,
            DefaultPostAction,
            DefaultDeleteAction,
            DefaultFilterParameter,
            DefaultSortParameter,
            DefaultSortDirectionParameter,
            DefaultPageParameter,
            DefaultItemsPerPageParameter,
            DefaultUpdateAction);
        var routes =  autoRouter.GetAutoroutes(routePrefix);
        if (routes != null) 
        {
            foreach (var v in routes)
            {
                AddRoute(appBuilder, routePrefix , v.Key, v.Value);
            }            
        }            
    }
    /// <summary>
    /// Adds autocontroller for DBContext.
    ///
    /// </summary>
    /// <typeparam name="T">The DBContext derived type</typeparam>
    /// <param name="appBuilder">The instance of ApplicationBuilder</param>
    /// <param name="routePrefix">start prefix for route</param>
    /// <param name="useLogging">log information for route</param>
    /// <param name="interactingType">Interacting type</param>
    /// <param name="authentificationPath">Application authentification path</param>
    /// <param name="accessDeniedPath">Application access denied page path</param>
    /// <param name="jsonOptions">Sets the JsonSerializerOptions</param>
    /// <param name="DefaultGetAction">The action path of default GET request. Default = Index</param>
    /// <param name="DefaultGetCountAction">The action path of default Count GET request. Default = Count</param>
    /// <param name="DefaultPostAction">The action path of default POST request. Default = Save</param>
    /// <param name="DefaultDeleteAction">The action path of default DELETE request. Default = Delete</param>
    /// <param name="DefaultFilterParameter">default = filter</param>
    /// <param name="DefaultSortParameter">default = sort</param>
    /// <param name="DefaultSortDirectionParameter">default = sortdirection</param>
    /// <param name="DefaultPageParameter">default = page</param>
    /// <param name="DefaultItemsPerPageParameter">default = size</param>
    /// <param name="DefaultUpdateAction">The action path of default PUT request. Default = Update</param>
    public static void UseAutoController<T>(
        this IApplicationBuilder appBuilder,
        string routePrefix,
        bool useLogging,
        InteractingType? interactingType,
        string authentificationPath,
        string accessDeniedPath,
        JsonSerializerOptions? jsonOptions = null,
        string DefaultGetAction = "Index",
        string DefaultGetCountAction = "Count",
        string DefaultPostAction = "Save",
        string DefaultDeleteAction = "Delete",
        string DefaultFilterParameter = "filter",
        string DefaultSortParameter = "sort",
        string DefaultSortDirectionParameter = "sortdirection",
        string DefaultPageParameter = "page",
        string DefaultItemsPerPageParameter = "size",
        string DefaultUpdateAction = "Update"
        ) where T : DbContext
    {
        if (appBuilder == null)
        {
            throw new ArgumentNullException(nameof(appBuilder));
        }
        var logger = GetOrCreateLogger(appBuilder, LogCategoryName);
        AutoRouterService<T> autoRouter = GetAutoRouterService<T>(appBuilder);
        if (useLogging)
        {
            autoRouter.AttachToLogger(logger);
        }
        autoRouter.GetAutoControllers(
            routePrefix,
            interactingType,
            authentificationPath,
            accessDeniedPath,
            jsonOptions,
            DefaultGetAction,
            DefaultGetCountAction,
            DefaultPostAction,
            DefaultDeleteAction,
            DefaultFilterParameter,
            DefaultSortParameter,
            DefaultSortDirectionParameter,
            DefaultPageParameter,
            DefaultItemsPerPageParameter,
            DefaultUpdateAction);
        var routes =  autoRouter.GetAutoroutes(routePrefix);
        if (routes != null) 
        {
            foreach (var v in routes)
            {
                AddRoute(appBuilder, routePrefix , v.Key, v.Value);
            }            
        }   
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
        IAutoControllerOptions options) where T : DbContext
    {
        UseAutoController<T>(appBuilder,
        options.RoutePrefix,
         options.LogInformation,
         options.InteractingType,
         options.AuthentificationPath,
         options.AccessDeniedPath,
         options.JsonSerializerOptions,
         options.DefaultGetAction,
         options.DefaultGetCountAction,
         options.DefaultPostAction,
         options.DefaultDeleteAction,
         options.DefaultFilterParameter,
         options.DefaultSortParameter,
         options.DefaultSortDirectionParameter,
         options.DefaultPageParameter,
         options.DefaultItemsPerPageParameter,
         options.DefaultUpdateAction);
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
