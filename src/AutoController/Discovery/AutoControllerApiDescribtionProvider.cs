using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;

namespace AutoController;

internal sealed class AutoControllerApiDescribtionProvider : IApiDescriptionProvider
{
    private readonly EndpointDataSource _endpointDataSource;

    public AutoControllerApiDescribtionProvider(EndpointDataSource endpointDataSource)
    {
        _endpointDataSource = endpointDataSource;
    }

    // Executes after ASP.NET Core
    public int Order => -800;

    public void OnProvidersExecuting(ApiDescriptionProviderContext context)
    {
         var endpoints = _endpointDataSource.Endpoints;

        foreach (var endpoint in endpoints)
        {
            if (endpoint is RouteEndpoint routeEndpoint)
            {
                var autocontrollerMetadata = endpoint.Metadata.GetMetadata<AutoControllerRouteMetadata>();
                if (autocontrollerMetadata != null)
                {
                    var apiDescription = CreateApiDescription(routeEndpoint, autocontrollerMetadata);
                    context.Results.Add(apiDescription);                    
                }
            }
        }
    }

    private static ApiDescription CreateApiDescription(RouteEndpoint routeEndpoint, AutoControllerRouteMetadata autocontrollerMetadata)
    {
        var apiDescription = new ApiDescription
        {
            HttpMethod = autocontrollerMetadata.Verb,
            GroupName = autocontrollerMetadata.Prefix,
            ActionDescriptor = new ActionDescriptor
            {
                DisplayName = autocontrollerMetadata.Action,
                EndpointMetadata = new List<object>() {
                autocontrollerMetadata.Template
            },
                RouteValues = new Dictionary<string, string?>
                {
                    // Swagger uses this to group endpoints together.
                    // Group methods together using the service name.
                    ["controller"] = autocontrollerMetadata.Controller,
                    ["action"] = autocontrollerMetadata.Action
                },
                //EndpointMetadata = autocontrollerMetadata.;// routeEndpoint.Metadata..ToList()
            }
        };
        var routeInfo = new Microsoft.AspNetCore.Mvc.Routing.AttributeRouteInfo
        {
            Name = autocontrollerMetadata.Template,
            Template = autocontrollerMetadata.Template
        };
        apiDescription.ActionDescriptor.AttributeRouteInfo=routeInfo;
        if (autocontrollerMetadata.InteractingType == InteractingType.XML)
        {
            apiDescription.SupportedRequestFormats.Add(new ApiRequestFormat { MediaType = "application/xml" });
            apiDescription.SupportedResponseTypes.Add(new ApiResponseType
            {
                ApiResponseFormats = { new ApiResponseFormat { MediaType = "application/xml" } },
                StatusCode = 200
            });
        }
        else
        {
            apiDescription.SupportedRequestFormats.Add(new ApiRequestFormat { MediaType = "application/json" });
            apiDescription.SupportedResponseTypes.Add(new ApiResponseType
            {
                ApiResponseFormats = { new ApiResponseFormat { MediaType = "application/json" } },
                StatusCode = 200
            });
        }
        return apiDescription;
    }
    public void OnProvidersExecuted(ApiDescriptionProviderContext context)
    {
        // no-op
    }
}
