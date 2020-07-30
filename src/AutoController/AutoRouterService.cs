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
    public class RouteKey
    {
        public string Path {get;set;}
        public HttpMethod HttpMethod {get;set;}

        public override string ToString()
        {
            return HttpMethod.ToString() + " " + Path;
        }

    }
    public class RouteParameters
    {
        public Type EntityType {get;set;}
        public uint ItemsPerPage {get;set;}
        public RequestDelegate Handler {get;set;}
    }

    public class AutoRouterService<T> where T: DbContext, IDisposable
    {
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
        private void LogInformation(string message)
        {
            if (logger != null)
            {
                logger.LogInformation(message);
            }
        }
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
        private void AddGetRoutesForEntity( string controllerName, string DefaultGetAction, Type givenType, InteractingType interactingType)
        {
            string basePath = _startRoutePath + controllerName;
            string countPath = basePath + "/Count";
            string defaultPath = basePath + "/" + DefaultGetAction;
            RouteKey rkeyDefault = new RouteKey() { Path = defaultPath, HttpMethod = HttpMethod.Get};
            RouteKey rkeyCount = new RouteKey() { Path = countPath, HttpMethod = HttpMethod.Get};
            if (!_autoroutes.ContainsKey(rkeyDefault))
            {
                RouteParameters rParam = new RouteParameters();
                rParam.EntityType = givenType;
                rParam.Handler = Handler.GetRequestDelegate("GetHandler",
                                                            new Type[] { typeof(T), givenType },
                                                            this,
                                                            new object[] { DatabaseType, _connectionString, interactingType, _jsonOptions });
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
                                                            new object[] { DatabaseType, _connectionString });
                _autoroutes.Add(rkeyCount, rParam);
                LogInformation(String.Format("Add route {0} for {1}", rkeyCount, givenType));
            }
        }
        private void AddPostRouteForEntity( string controllerName, string DefaultPostAction, Type givenType, InteractingType interactingType)
        {
            string basePath = _startRoutePath + controllerName;
            string defaultPath = basePath + "/" + DefaultPostAction;
            RouteKey rkeyDefault = new RouteKey() { Path = defaultPath, HttpMethod = HttpMethod.Post};
            if (!_autoroutes.ContainsKey(rkeyDefault))
            {
                RouteParameters rParam = new RouteParameters();
                rParam.EntityType = givenType;
                rParam.Handler = Handler.GetRequestDelegate("PostHandler",
                                                            new Type[] { typeof(T), givenType },
                                                            this,
                                                            new object[] { DatabaseType, _connectionString, interactingType, _jsonOptions });
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
                    AddGetRoutesForEntity( controllerName, r.DefaultGetAction, givenType, usedInteractingType);
                    AddPostRouteForEntity( controllerName, "Save", givenType, usedInteractingType);
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
        public void GetAutoControllers(
            string routePrefix,
            DatabaseTypes databaseType,
            string connectionString,
            InteractingType? interactingType,
            JsonSerializerOptions jsonSerializerOptions = null)
        {
            _connectionString = connectionString;
            DatabaseType = databaseType;
            _routePrefix = routePrefix;
            _defaultInteractingType = interactingType;
            _jsonOptions = jsonSerializerOptions;
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