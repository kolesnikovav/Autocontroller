using System;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoController
{
    public static class AutoControllerExtention
    {
        private const string LogCategoryName = "AutoController";
        /// <summary>
        /// Adds AutoController as singletone service and register it in Dependency Injection
        ///
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        public static void AddAutoController(this IServiceCollection services)
        {
            services.AddSingleton(typeof(AutoRouterService));
        }
        private static AutoRouterService GetAutoRouterService(IApplicationBuilder builder)
        {
            return (AutoRouterService)builder.ApplicationServices.GetService(typeof(AutoRouterService));
        }
        /// <summary>
        /// Adds autocontroller.
        ///
        /// </summary>
        /// <param name="appBuilder">The <see cref="ApplicationBuilder"/>.</param>
        /// <param name="relatedType">The type to be creating for controller</param>
        /// <param name="routePrefix">start prefix for route</param>
        public static void UseAutoController(
            this IApplicationBuilder appBuilder,
            Type relatedType, string routePrefix)
        {
            if (appBuilder == null)
            {
                throw new ArgumentNullException(nameof(appBuilder));
            }
            var logger = GetOrCreateLogger(appBuilder, LogCategoryName);
            AutoRouterService autoRouter = GetAutoRouterService(appBuilder);
            autoRouter.GetAutoControllers(relatedType, routePrefix);
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