using System;
using System.Text.Json;

namespace AutoController
{
    /// <summary>
    /// Decribe database type for your DBContext
    /// </summary>
    public class AutoControllerOptions: IAutoControllerOptions
    {
        /// <summary>
        /// Start prefix segment for route
        /// </summary>
        public string RoutePrefix {get;set;}
        /// <summary>
        /// Controller default action for get request
        /// </summary>
        public string DefaultGetAction {get;set;} = "Index";
        /// <summary>
        /// Controller default action for retrive count of objects
        /// </summary>
        public string DefaultGetCountAction {get;set;} = "Count";
        /// <summary>
        /// Keyword for request parameter for filering items
        /// </summary>
        public string DefaultFilterParameter {get;set;} = "filter";
        /// <summary>
        /// Keyword for request parameter for sorting items
        /// </summary>
        public string DefaultSortParameter {get;set;} = "sort";
        /// <summary>
        /// Keyword for request parameter for sortdirection items
        /// </summary>
        public string DefaultSortDirectionParameter {get;set;} = "sortdirection";
        /// <summary>
        ///
        /// </summary>
        public string DefaultPageParameter {get;set;} = "page";
        /// <summary>
        ///
        /// </summary>
        public string DefaultItemsPerPageParameter {get;set;} = "size";
        /// <summary>
        /// Controller default action for post request
        /// </summary>
        public string DefaultPostAction {get;set;} = "Save";
        /// <summary>
        /// Database type for DBContext
        /// </summary>
        public DatabaseTypes DatabaseType {get;set;}
        /// <summary>
        /// Connection string for DBContext
        /// </summary>
        public string ConnectionString {get;set;}
        /// <summary>
        /// Use logger for autocontroller
        /// </summary>
        public bool LogInformation {get;set;}
        /// <summary>
        /// Interacting Type for autocontrolles
        /// </summary>
        public InteractingType? InteractingType {get;set;}
        /// <summary>
        /// Options for json serialization and deserialization
        /// </summary>
        public JsonSerializerOptions JsonSerializerOptions {get;set;}
        /// <summary>
        /// Autentification page path
        /// </summary>
        public string AuthentificationPath {get;set;}
        /// <summary>
        /// Access denied page path
        /// </summary>
        public string  AccessDeniedPath {get;set;}
        /// <summary>
        /// Parameterless constructor
        /// </summary>
        public AutoControllerOptions() {}

    }
}