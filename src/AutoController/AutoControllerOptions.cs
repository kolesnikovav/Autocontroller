using System.Collections.Generic;
using System.Text.Json;
using MySqlX.XDevAPI.Common;

namespace AutoController;
/// <summary>
/// Decribe database type for your DBContext
/// </summary>
public class AutoControllerOptions : IAutoControllerOptions
{
    /// <summary>
    /// indicates that the service is running in development mode
    /// </summary>    
    public bool IsDevelopment { get; set; }=false;
    /// <summary>
    /// Start prefix segment for route
    /// </summary>
    public string RoutePrefix { get; set; }=string.Empty;
    /// <summary>
    /// Controller default action for get request
    /// </summary>
    public string DefaultGetAction { get; set; } = "Index";
    /// <summary>
    /// Controller default action for retrive count of objects
    /// </summary>
    public string DefaultGetCountAction { get; set; } = "Count";
    /// <summary>
    /// Keyword for request parameter for filering items
    /// </summary>
    public string DefaultFilterParameter { get; set; } = "filter";
    /// <summary>
    /// Keyword for request parameter for sorting items
    /// </summary>
    public string DefaultSortParameter { get; set; } = "sort";
    /// <summary>
    /// Keyword for request parameter for sortdirection items
    /// </summary>
    public string DefaultSortDirectionParameter { get; set; } = "sortdirection";
    /// <summary>
    ///
    /// </summary>
    public string DefaultPageParameter { get; set; } = "page";
    /// <summary>
    ///
    /// </summary>
    public string DefaultItemsPerPageParameter { get; set; } = "size";
    /// <summary>
    /// Controller default action for post request
    /// </summary>
    public string DefaultPostAction { get; set; } = "Save";
    /// <summary>
    ///
    /// </summary>
    public string DefaultUpdateAction { get; set; } = "Update";
    /// <summary>
    ///
    /// </summary>
    public string DefaultDeleteAction { get; set; } = "Delete";
    /// <summary>
    /// Database type for DBContext
    /// </summary>
    public DatabaseTypes DatabaseType { get; set; }
    /// <summary>
    /// Connection string for DBContext
    /// </summary>
    public string ConnectionString { get; set; }=string.Empty;
    /// <summary>
    /// Use logger for autocontroller
    /// </summary>
    public bool LogInformation { get; set; }
    /// <summary>
    /// Interacting Type for autocontrolles
    /// </summary>
    public InteractingType InteractingType { get; set; } = InteractingType.JSON;
    /// <summary>
    /// Options for json serialization and deserialization
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }
    /// <summary>
    /// Autentification page path
    /// </summary>
    public string AuthentificationPath { get; set; } = string.Empty;
    /// <summary>
    /// Access denied page path
    /// </summary>
    public string AccessDeniedPath { get; set; }= string.Empty;
    /// <summary>
    /// Parameterless constructor
    /// </summary>
    public Dictionary<string, RequestParamName> RequestParamNames 
    { 
        get 
        {
            var result = new Dictionary<string, RequestParamName>
            {
                { "page", new RequestParamName() { UserDefinedValue = DefaultPageParameter, TypeToCast = typeof(uint) } },
                { "size", new RequestParamName() { UserDefinedValue = DefaultItemsPerPageParameter, TypeToCast = typeof(uint) } },
                { "filter", new RequestParamName() { UserDefinedValue = DefaultFilterParameter, TypeToCast = typeof(string) } },
                { "sort", new RequestParamName() { UserDefinedValue = DefaultSortParameter, TypeToCast = typeof(string) } },
                { "sortdirection", new RequestParamName() { UserDefinedValue = DefaultSortDirectionParameter, TypeToCast = typeof(string) } }
            };
            return result;
        }
    } 
    /// <summary>
    /// Parameterless constructor
    /// </summary>
    public AutoControllerOptions() { }

}
