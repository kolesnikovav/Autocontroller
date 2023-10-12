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
        var apiDescription = new ApiDescription();
        apiDescription.HttpMethod = autocontrollerMetadata.Verb;
        apiDescription.GroupName = autocontrollerMetadata.Prefix;

        apiDescription.ActionDescriptor = new ActionDescriptor
        {
            RouteValues = new Dictionary<string, string?>
            {
                // Swagger uses this to group endpoints together.
                // Group methods together using the service name.
                ["controller"] = autocontrollerMetadata.Controller,
                ["action"] = autocontrollerMetadata.Action
            },
            //EndpointMetadata = autocontrollerMetadata.;// routeEndpoint.Metadata..ToList()
        };
        apiDescription.SupportedRequestFormats.Add(new ApiRequestFormat { MediaType = "application/json" });
                // apiDescription.SupportedResponseTypes.Add(new ApiResponseType
                // {
                //     ApiResponseFormats = { new ApiResponseFormat { MediaType = "application/json" } },
                //     ModelMetadata = new GrpcModelMetadata(ModelMetadataIdentity.ForType(methodDescriptor.OutputType.ClrType)),
                //     StatusCode = 200
                // });
        //         apiDescription.SupportedResponseTypes.Add(new ApiResponseType
        //         {
        //             ApiResponseFormats = { new ApiResponseFormat { MediaType = "application/json" } },
        //             ModelMetadata = new GrpcModelMetadata(ModelMetadataIdentity.ForType(typeof(Google.Rpc.Status))),
        //             IsDefaultResponse = true
        //         });
        //         var explorerSettings = routeEndpoint.Metadata.GetMetadata<ApiExplorerSettingsAttribute>();
        //         if (explorerSettings != null)
        //         {
        //             apiDescription.GroupName = explorerSettings.GroupName;
        //         }

        //         // var methodMetadata = routeEndpoint.Metadata.GetMetadata<GrpcMethodMetadata>()!;
        //         // var httpRoutePattern = HttpRoutePattern.Parse(pattern);
        //         // var routeParameters = ServiceDescriptorHelpers.ResolveRouteParameterDescriptors(httpRoutePattern.Variables, methodDescriptor.InputType);

        //         apiDescription.RelativePath = ResolvePath(httpRoutePattern, routeParameters);

        //         foreach (var routeParameter in routeParameters)
        //         {
        //             // var field = routeParameter.Value.DescriptorsPath.Last();
        //             // var parameterName = ServiceDescriptorHelpers.FormatUnderscoreName(field.Name, pascalCase: true, preservePeriod: false);
        //             // var propertyInfo = field.ContainingType.ClrType.GetProperty(parameterName);

        //             // If from a property, create model as property to get its XML comments.
        //             // var identity = propertyInfo != null
        //             //     ? ModelMetadataIdentity.ForProperty(propertyInfo, MessageDescriptorHelpers.ResolveFieldType(field), field.ContainingType.ClrType)
        //             //     : ModelMetadataIdentity.ForType(MessageDescriptorHelpers.ResolveFieldType(field));

        //             apiDescription.ParameterDescriptions.Add(new ApiParameterDescription
        //             {
        //                 Name = routeParameter.Value.JsonPath,
        //                 //ModelMetadata = new GrpcModelMetadata(identity),
        //                 Source = BindingSource.Path,
        //                 DefaultValue = string.Empty
        //             });
        //         }

        //         // var bodyDescriptor = ServiceDescriptorHelpers.ResolveBodyDescriptor(httpRule.Body, methodMetadata.ServiceType, methodDescriptor);
        //         // if (bodyDescriptor != null)
        //         // {
        //         //     // If from a property, create model as property to get its XML comments.
        //         //     var identity = bodyDescriptor.PropertyInfo != null
        //         //         ? ModelMetadataIdentity.ForProperty(bodyDescriptor.PropertyInfo, bodyDescriptor.PropertyInfo.PropertyType, bodyDescriptor.PropertyInfo.DeclaringType!)
        //         //         : ModelMetadataIdentity.ForType(bodyDescriptor.Descriptor.ClrType);

        //         //     // Or if from a parameter, create model as parameter to get its XML comments.
        //         //     var parameterDescriptor = bodyDescriptor.ParameterInfo != null
        //         //         ? new ControllerParameterDescriptor { ParameterInfo = bodyDescriptor.ParameterInfo }
        //         //         : null;

        //         //     apiDescription.ParameterDescriptions.Add(new ApiParameterDescription
        //         //     {
        //         //         Name = "Input",
        //         //         ModelMetadata = new GrpcModelMetadata(identity),
        //         //         Source = BindingSource.Body,
        //         //         ParameterDescriptor = parameterDescriptor!
        //         //     });
        //         // }

        //         // var queryParameters = ServiceDescriptorHelpers.ResolveQueryParameterDescriptors(routeParameters, methodDescriptor, bodyDescriptor?.Descriptor, bodyDescriptor?.FieldDescriptor);
        //         // foreach (var queryDescription in queryParameters)
        //         // {
        //         //     var fieldType = MessageDescriptorHelpers.ResolveFieldType(queryDescription.Value);
        //         //     if (queryDescription.Value.IsRepeated)
        //         //     {
        //         //         fieldType = typeof(List<>).MakeGenericType(fieldType);
        //         //     }

        //         //     apiDescription.ParameterDescriptions.Add(new ApiParameterDescription
        //         //     {
        //         //         Name = queryDescription.Key,
        //         //         ModelMetadata = new GrpcModelMetadata(ModelMetadataIdentity.ForType(fieldType)),
        //         //         Source = BindingSource.Query,
        //         //         DefaultValue = string.Empty
        //         //     });
        //         // }

        return apiDescription;
    }

    //     private static string ResolvePath(HttpRoutePattern httpRoutePattern, Dictionary<string, RouteParameter> routeParameters)
    //     {
    //         var sb = new StringBuilder();
    //         // for (var i = 0; i < httpRoutePattern.Segments.Count; i++)
    //         // {
    //         //     if (sb.Length > 0)
    //         //     {
    //         //         sb.Append('/');
    //         //     }
    //         //     var routeParameter = routeParameters.SingleOrDefault(kvp => kvp.Value.RouteVariable.StartSegment == i).Value;
    //         //     if (routeParameter != null)
    //         //     {
    //         //         sb.Append('{');
    //         //         sb.Append(routeParameter.JsonPath);
    //         //         sb.Append('}');

    //         //         // Skip segments if variable is multiple segment.
    //         //         i = routeParameter.RouteVariable.EndSegment - 1;
    //         //     }
    //         //     else
    //         //     {
    //         //         sb.Append(httpRoutePattern.Segments[i]);
    //         //     }
    //         // }
    //         // if (httpRoutePattern.Verb != null)
    //         // {
    //         //     sb.Append(':');
    //         //     sb.Append(httpRoutePattern.Verb);
    //         // }
    //         return sb.ToString();
    //     }

    public void OnProvidersExecuted(ApiDescriptionProviderContext context)
    {
        // no-op
    }
}
