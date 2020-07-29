using System;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AutoController
{
    public static class AutoControllerExtention
    {
        private const string LogCategoryName = "AutoController";
        /// <summary>
        /// Adds AutoController as singletone service and register it in Dependency Injection
        ///
        /// </summary>
        /// <param name="<T>">The type of DBContext/>.</param>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        public static void AddAutoController<T>(this IServiceCollection services) where T: DbContext
        {
            services.AddSingleton(typeof(AutoRouterService<T>));
        }
        private static AutoRouterService<T> GetAutoRouterService<T>(IApplicationBuilder builder) where T: DbContext
        {
            return (AutoRouterService<T>)builder.ApplicationServices.GetService(typeof(AutoRouterService<T>));
        }
        /// <summary>
        /// Adds autocontroller for DBContext.
        ///
        /// </summary>
        /// <param name=<T>The DBContext derived type</param>
        /// <param name="appBuilder">The <see cref="ApplicationBuilder"/>.</param>
        /// <param name="relatedType">The type to be creating for controller</param>
        /// <param name="routePrefix">start prefix for route</param>
        /// <param name="useLogging">log information for route</param>
        /// <param name="databaseType">The type of your database</param>
        /// <param name="connectionString">Database connection string</param>
        public static void UseAutoController<T>(
            this IApplicationBuilder appBuilder,
            Type relatedType, string routePrefix, bool useLogging, DatabaseTypes databaseType, string connectionString) where T: DbContext
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
            autoRouter.GetAutoControllers(relatedType, routePrefix, databaseType, connectionString);
            foreach(var route in autoRouter._autoroutes)
            {
                //appBuilder.UseRouter()
                //appBuilder.UseRouter.UseMiddleware().UseRouter()
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
}