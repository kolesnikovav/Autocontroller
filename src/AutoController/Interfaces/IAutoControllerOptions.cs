using System.Collections.Generic;
using System.Text.Json;

namespace AutoController;

/// <summary>
/// Options for autocontroller
/// </summary>
public interface IAutoControllerOptions
{

    /// <summary>
    /// indicates that the service is running in development mode
    /// </summary>
    bool IsDevelopment { get; set; } 
    /// <summary>
    /// Start prefix segment for route
    /// </summary>    
    string RoutePrefix { get; set; }
    /// <summary>
    /// The action path of default GET request
    /// </summary>
    string DefaultGetAction { get; set; }
    /// <summary>
    /// The action path of default Count GET request
    /// </summary>
    string DefaultGetCountAction { get; set; }
    /// <summary>
    ///
    /// </summary>
    string DefaultFilterParameter { get; set; }
    /// <summary>
    ///
    /// </summary>
    string DefaultSortParameter { get; set; }
    /// <summary>
    ///
    /// </summary>
    string DefaultSortDirectionParameter { get; set; }
    /// <summary>
    ///
    /// </summary>
    string DefaultPageParameter { get; set; }
    /// <summary>
    ///
    /// </summary>
    string DefaultItemsPerPageParameter { get; set; }
    /// <summary>
    ///
    /// </summary>
    string DefaultPostAction { get; set; }
    /// <summary>
    ///
    /// </summary>
    string DefaultDeleteAction { get; set; }
    /// <summary>
    ///
    /// </summary>
    string DefaultUpdateAction { get; set; }
    /// <summary>
    /// Database type for DBContext
    /// </summary>
    DatabaseTypes DatabaseType { get; set; }
    /// <summary>
    /// Connection string for DBContext
    /// </summary>
    string ConnectionString { get; set; }
    /// <summary>
    /// Use logger for autocontroller
    /// </summary>
    bool LogInformation { get; set; }
    /// <summary>
    /// Interacting Type for autocontrolles
    /// </summary>
    InteractingType InteractingType { get; set; }
    /// <summary>
    /// Options for json serialization and deserialization
    /// </summary>
    JsonSerializerOptions? JsonSerializerOptions { get; set; }
    /// <summary>
    /// Authentification page path
    /// </summary>
    string AuthentificationPath { get; set; }
    /// <summary>
    /// Access denied page path
    /// </summary>
    string AccessDeniedPath { get; set; }
    /// <summary>
    /// Get request parameter names (page, size, filter,...) 
    /// </summary>
    Dictionary<string, RequestParamName> RequestParamNames { get;}
}
