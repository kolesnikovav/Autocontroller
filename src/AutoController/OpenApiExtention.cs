using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoController;


/// <summary>
/// OpenApiExtention methods for AutoController
/// </summary> <summary>
/// 
/// </summary>
public static class OpenApiExtention
{
    /// <summary>
    /// Add Open Api definitions for AutoController routes
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns> <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddAutoControllerOpenApiDefinition(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient<IApiDescriptionProvider, AutoControllerApiDescribtionProvider>());
        return services;
    }    
}