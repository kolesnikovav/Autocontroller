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

namespace AutoController
{
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
        public static void AddAutoController<T>(this IServiceCollection services) where T: DbContext
        {
            services.AddSingleton(typeof(AutoRouterService<T>));
        }
        private static AutoRouterService<T> GetAutoRouterService<T>(IApplicationBuilder builder) where T: DbContext
        {
            return (AutoRouterService<T>)builder.ApplicationServices.GetService(typeof(AutoRouterService<T>));
        }
        private static void AddRoute(IApplicationBuilder builder, RouteKey key, RouteParameters parameters)
        {
            var routeHandler = new RouteHandler(parameters.Handler);
            var routeBuilder = new RouteBuilder(builder, routeHandler);
            routeBuilder.MapRoute(key.ToString(), key.Path);
            builder.UseRouter(routeBuilder.Build());
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
        /// <param name="DefaultGetAction">Sets the JsonSerializerOptions</param>
        /// <param name="DefaultGetCountAction">Sets the JsonSerializerOptions</param>
        /// <param name="DefaultPostAction">Sets the JsonSerializerOptions</param>
        /// <param name="DefaultFilterParameter">Sets the JsonSerializerOptions</param>
        /// <param name="DefaultSortParameter">Sets the JsonSerializerOptions</param>
        /// <param name="DefaultSortDirectionParameter">Sets the JsonSerializerOptions</param>
        /// <param name="DefaultPageParameter">Sets the JsonSerializerOptions</param>
        /// <param name="DefaultItemsPerPageParameter">Sets the JsonSerializerOptions</param>
        public static void UseAutoController<T>(
            this IApplicationBuilder appBuilder,
            string routePrefix,
            bool useLogging,
            DatabaseTypes databaseType,
            string connectionString,
            InteractingType? interactingType,
            string authentificationPath,
            string accessDeniedPath,
            JsonSerializerOptions jsonOptions = null,
            string DefaultGetAction = "Index",
            string DefaultGetCountAction = "Count",
            string DefaultPostAction = "Save",
            string DefaultFilterParameter = "filter",
            string DefaultSortParameter = "sort",
            string DefaultSortDirectionParameter = "sortdirection",
            string DefaultPageParameter = "page",
            string DefaultItemsPerPageParameter = "size"
            ) where T: DbContext
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
                jsonOptions);
            foreach(var route in autoRouter._autoroutes)
            {
                AddRoute(appBuilder, route.Key, route.Value);
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
             options.DatabaseType,
             options.ConnectionString,
             options.InteractingType,
             options.AuthentificationPath,
             options.AccessDeniedPath,
             options.JsonSerializerOptions,
             options.DefaultGetAction,
             options.DefaultGetCountAction,
             options.DefaultPostAction,
             options.DefaultFilterParameter,
             options.DefaultSortParameter,
             options.DefaultSortDirectionParameter,
             options.DefaultPageParameter,
             options.DefaultItemsPerPageParameter);
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
}