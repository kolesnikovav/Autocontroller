using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;

namespace AutoController
{
    /// <summary>
    /// Discribe request path with http request type
    /// </summary>
    public class RouteKey
    {
        /// <summary>
        /// Request path
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// Http request method
        /// Currently, GET and POST supported
        /// </summary>
        public HttpMethod HttpMethod { get; set; }
        /// <summary>
        /// route key string presentation
        /// </summary>

        public override string ToString()
        {
            return HttpMethod.ToString() + " " + Path;
        }

    }
    /// <summary>
    /// Route parameter for handling request
    /// </summary>
    public class RouteParameters
    {
        /// <summary>
        /// Type of Entity in DBContext
        /// </summary>
        public Type EntityType { get; set; }
        /// <summary>
        ///
        /// </summary>
        public uint ItemsPerPage { get; set; }
        /// <summary>
        /// handler for request
        /// </summary>
        public RequestDelegate Handler { get; set; }
    }
    /// <summary>
    /// Service that handle requests for Entityes
    /// </summary>

    public class AutoRouterService<T> where T: DbContext, IDisposable
    {
        /// <summary>
        /// The Dictionary with all used routes
        /// </summary>
        public Dictionary<RouteKey, RouteParameters> _autoroutes = new Dictionary<RouteKey, RouteParameters>();
        private string _routePrefix;
        private string _startRoutePath;
        private InteractingType?  _defaultInteractingType;
        private JsonSerializerOptions  _jsonOptions;
        private Type MapToControllerAttributeType = typeof(MapToControllerAttribute);
        private ILogger logger;
        private Type MapToControllerGetParamAttributeType = typeof(MapToControllerGetParamAttribute);
        private Type  KeyAttributeType  = typeof(KeyAttribute);
        private string  _connectionString;
        private string  _authentificationPath;
        private string  _accessDeniedPath;
        private  string _defaultGetAction;
        private  string _defaultGetCountAction;
        private  string _defaultPostAction;
        private  string _defaultFilterParameter;
        private  string _defaultSortParameter;
        private string _defaultSortDirectionParameter;
        private  string _defaultPageParameter;
        private string _defaultItemsPerPageParameter;
        private void LogInformation(string message)
        {
            if (logger != null)
            {
                logger.LogInformation(message);
            }
        }
        /// <summary>
        /// Database type for autocontroller
        /// </summary>
        public DatabaseTypes DatabaseType {get;set;}
        /// <summary>
        /// Attach to logger.
        /// </summary>
        /// <param name="logger">The logger to attach </param>
        public void AttachToLogger(ILogger logger)
        {
            this.logger = logger;
        }
        private void AddRoutesForProperties(Type givenType, string routeClassName, uint itemsPerPage)
        {
            foreach (PropertyInfo pInfo in givenType.GetProperties())
            {
                MapToControllerGetParamAttribute b = pInfo.GetCustomAttribute(MapToControllerGetParamAttributeType) as MapToControllerGetParamAttribute;
                KeyAttribute k = pInfo.GetCustomAttribute(KeyAttributeType) as KeyAttribute;
                if (b != null)
                {
                    string r = String.IsNullOrWhiteSpace(b.ParamName) ? pInfo.Name : b.ParamName;
                    string optional = b.Optional ? "?" : String.Empty;
                    string route = routeClassName + "/{" + r + optional + "}";
                    //"{controller}/{action}/{property?}"
                    RouteKey rkey = new RouteKey() {Path = route, HttpMethod = HttpMethod.Get};
                    _autoroutes.Add(
                        rkey,
                        new RouteParameters() { EntityType = pInfo.PropertyType, ItemsPerPage = itemsPerPage}
                        );
                    LogInformation(String.Format("Add route {0} for {1} type {2}", rkey, pInfo.Name, pInfo.PropertyType));

                }
                if (k != null)
                {
                    string r = String.IsNullOrWhiteSpace(b.ParamName) ? pInfo.Name : b.ParamName;
                    string route = routeClassName + "/{" + r +"}";
                    //"{controller}/{action}/{property}"
                    RouteKey rkey = new RouteKey() {Path = route, HttpMethod = HttpMethod.Post};
                    _autoroutes.Add(
                        rkey,
                        new RouteParameters() { EntityType = pInfo.PropertyType}
                        );
                    LogInformation(String.Format("Add route {0} for {1} type {2}", rkey, pInfo.Name, pInfo.PropertyType));
                }
            }
        }
        private void AddGetRoutesForEntity( string controllerName, Type givenType, InteractingType interactingType, bool allowAnonimus)
        {
            string basePath = _startRoutePath + controllerName;
            string countPath = basePath + "/" + _defaultGetCountAction;
            string defaultPath = basePath + "/" + _defaultGetAction;
            RouteKey rkeyDefault = new RouteKey() { Path = defaultPath, HttpMethod = HttpMethod.Get};
            RouteKey rkeyCount = new RouteKey() { Path = countPath, HttpMethod = HttpMethod.Get};
            if (!_autoroutes.ContainsKey(rkeyDefault))
            {
                RouteParameters rParam = new RouteParameters();
                rParam.EntityType = givenType;
                rParam.Handler = Handler.GetRequestDelegate("GetHandler",
                                                            new Type[] { typeof(T), givenType },
                                                            this,
                                                            new object[] { DatabaseType,
                                                             _connectionString,
                                                              interactingType,
                                                              allowAnonimus,
                                                              _authentificationPath,
                                                              _accessDeniedPath,
                                                             _jsonOptions });
                _autoroutes.Add(rkeyDefault, rParam);
                LogInformation(String.Format("Add route {0} for {1}", rkeyDefault, givenType));
            }
            if (!_autoroutes.ContainsKey(rkeyCount))
            {
                RouteParameters rParam = new RouteParameters();
                rParam.EntityType = givenType;
                rParam.Handler = Handler.GetRequestDelegate("GetCountOf",
                                                            new Type[] { typeof(T), givenType },
                                                            this,
                                                            new object[] { DatabaseType,
                                                            _connectionString,
                                                            allowAnonimus,
                                                            _authentificationPath,
                                                            _accessDeniedPath });
                _autoroutes.Add(rkeyCount, rParam);
                LogInformation(String.Format("Add route {0} for {1}", rkeyCount, givenType));
            }
        }
        private void AddPostRouteForEntity( string controllerName, Type givenType, InteractingType interactingType)
        {
            string basePath = _startRoutePath + controllerName;
            string defaultPath = basePath + "/" + _defaultPostAction;
            RouteKey rkeyDefault = new RouteKey() { Path = defaultPath, HttpMethod = HttpMethod.Post};
            if (!_autoroutes.ContainsKey(rkeyDefault))
            {
                RouteParameters rParam = new RouteParameters();
                rParam.EntityType = givenType;
                rParam.Handler = Handler.GetRequestDelegate("PostHandler",
                                                            new Type[] { typeof(T), givenType },
                                                            this,
                                                            new object[] { DatabaseType,
                                                                          _connectionString,
                                                                          interactingType,
                                                                          _authentificationPath,
                                                                          _accessDeniedPath,
                                                                          _jsonOptions });
                _autoroutes.Add(rkeyDefault, rParam);
                LogInformation(String.Format("Add route {0} for {1}", rkeyDefault, givenType));
            }
        }
        private void ProcessType (Type givenType)
        {
            if (givenType.IsClass)
            {
                MapToControllerAttribute r = (MapToControllerAttribute)givenType.GetCustomAttribute(MapToControllerAttributeType);
                if (r != null)
                {
                    InteractingType usedInteractingType = _defaultInteractingType == null ? r.InteractingType : (InteractingType)_defaultInteractingType;
                    string controllerName = String.IsNullOrWhiteSpace(r.ControllerName) ? givenType.Name : r.ControllerName;
                    AddGetRoutesForEntity( controllerName, givenType, usedInteractingType, r.AllowAnonimus);
                    AddPostRouteForEntity( controllerName, givenType, usedInteractingType);
                }
            }
            if (givenType.IsGenericType)
            {
                foreach (Type currentType in givenType.GetGenericArguments())
                {
                    ProcessType (currentType);
                }
            }
        }
        /// <summary>
        /// Using System.Reflection generates api controller for given type and properties
        /// By default, Controller name is the same as class name
        /// </summary>
        /// <param name="routePrefix">Prefix segment for controller</param>
        /// <param name="databaseType">Database type for DBContext</param>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="interactingType">Designates interacting type with autocontroller. If null, interacting type of entity will be applied</param>
        /// <param name="DefaultGetAction">Sets the JsonSerializerOptions</param>
        /// <param name="jsonSerializerOptions">JsonSerializerOptions that will be applied during interacting</param>
        /// <param name="authentificationPath">Autentification page path</param>
        /// <param name="accessDeniedPath">Access denied page path</param>
        /// <param name="DefaultGetCountAction">Sets the JsonSerializerOptions</param>
        /// <param name="DefaultPostAction">Sets the JsonSerializerOptions</param>
        /// <param name="DefaultFilterParameter">Sets the JsonSerializerOptions</param>
        /// <param name="DefaultSortParameter">Sets the JsonSerializerOptions</param>
        /// <param name="DefaultSortDirectionParameter">Sets the JsonSerializerOptions</param>
        /// <param name="DefaultPageParameter">Sets the JsonSerializerOptions</param>
        /// <param name="DefaultItemsPerPageParameter">Sets the JsonSerializerOptions</param>
        public void GetAutoControllers(
            string routePrefix,
            DatabaseTypes databaseType,
            string connectionString,
            InteractingType? interactingType,
            string authentificationPath,
            string accessDeniedPath,
            JsonSerializerOptions jsonSerializerOptions = null,
            string DefaultGetAction = "Index",
            string DefaultGetCountAction = "Count",
            string DefaultPostAction = "Save",
            string DefaultFilterParameter = "filter",
            string DefaultSortParameter = "sort",
            string DefaultSortDirectionParameter = "sortdirection",
            string DefaultPageParameter = "page",
            string DefaultItemsPerPageParameter = "size")
        {
            _connectionString = connectionString;
            DatabaseType = databaseType;
            _routePrefix = routePrefix;
            _defaultInteractingType = interactingType;
            _authentificationPath = authentificationPath;
            _accessDeniedPath = accessDeniedPath;
            _jsonOptions = jsonSerializerOptions;
            _defaultGetAction = DefaultGetAction;
            _defaultGetCountAction = DefaultGetCountAction;
            _defaultPostAction = DefaultPostAction;
            _defaultFilterParameter = DefaultFilterParameter;
            _defaultSortParameter = DefaultSortParameter;
            _defaultSortDirectionParameter = DefaultSortDirectionParameter;
            _defaultPageParameter = DefaultPageParameter;
            _defaultItemsPerPageParameter = DefaultItemsPerPageParameter;
            _startRoutePath = String.IsNullOrWhiteSpace(_routePrefix) ? String.Empty : _routePrefix + "/";
            ProcessType(typeof(T));
            PropertyInfo[] p = typeof(T).GetProperties();
            foreach (PropertyInfo t in p)
            {
                ProcessType(t.PropertyType);
            }
        }
    }
}