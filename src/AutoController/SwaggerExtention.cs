using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoController;
/// <summary>
/// 
/// </summary> <summary>
/// 
/// </summary>

public static class SwaggerExtention
{
        /// <summary>
        /// Extention of IServiceCollection method for add ApiExporer
        /// </summary>
        /// <param name="services"></param>
        /// <returns>Application services collection </returns>

        public static IServiceCollection AddAutoControllerApiExplorer(this IServiceCollection services)
        {
             //services.TryAddSingleton<<IActionDescriptorCollectionProvider, DefaultActionDescriptorCollectionProvider>();
             //services.AddEndpointsApiExplorer();
             return services;
        }

}