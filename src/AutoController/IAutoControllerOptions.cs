using System;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoController
{
        /// <summary>
        /// Options for autocontroller
        /// </summary>
    public interface IAutoControllerOptions
    {
        /// <summary>
        /// Start prefix segment for route
        /// </summary>
        string RoutePrefix {get;set;}
        string DefaultGetAction {get;set;}
        string DefaultGetCountAction {get;set;}
        string DefaultFilterParameter {get;set;}
        string DefaultSortParameter {get;set;}
        string DefaultSortDirectionParameter {get;set;}
        string DefaultPostAction {get;set;}
        /// <summary>
        /// Database type for DBContext
        /// </summary>
        DatabaseTypes DatabaseType {get;set;}
        /// <summary>
        /// Connection string for DBContext
        /// </summary>
        string ConnectionString {get;set;}
        /// <summary>
        /// Use logger for autocontroller
        /// </summary>
        bool LogInformation {get;set;}
        /// <summary>
        /// Interacting Type for autocontrolles
        /// </summary>
        InteractingType? InteractingType {get;set;}
        /// <summary>
        /// Options for json serialization and deserialization
        /// </summary>
        JsonSerializerOptions JsonSerializerOptions {get;set;}
    }
}