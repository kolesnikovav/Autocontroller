using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        private void ProcessType (Type givenType)
        {
            if (givenType.IsClass)
            {
                MapToControllerAttribute r = (MapToControllerAttribute)givenType.GetCustomAttribute(MapToControllerAttributeType);
                if (r != null)
                {
                    string routeName = String.IsNullOrWhiteSpace(_routePrefix) ? String.Empty : _routePrefix + "/";
                    routeName += String.IsNullOrWhiteSpace(r.ControllerName) ? givenType.Name : r.ControllerName;
                    string collectionName = r.CollectionName;
                    routeName +="/" +r.DefaultGetAction ;
                    string routeNameWithPage = routeName;
                    if (r.ItemsPerPage > 0) // no infinite scroll
                    {
                        routeNameWithPage +="/{page?}" ;
                        RouteKey rkeyWithPage = new RouteKey() { Path = routeNameWithPage, HttpMethod = HttpMethod.Get};
                        if (!_autoroutes.ContainsKey(rkeyWithPage))
                        {
                            RouteParameters rParam = new RouteParameters();
                            rParam.EntityType = givenType;
                            rParam.ItemsPerPage = r.ItemsPerPage;
                            rParam.Handler = Handler.GetRequestDelegate("GetHandlerPage",
                                                                        new Type[] {typeof(T), givenType},
                                                                        this,
                                                                        new object[] {DatabaseType, _connectionString,(uint)20,r.ItemsPerPage});
                            _autoroutes.Add(rkeyWithPage, rParam);
                            LogInformation(String.Format("Add route {0} for {1}", rkeyWithPage, givenType));
                        }
                    }
                    RouteKey rkey = new RouteKey() { Path = routeName, HttpMethod = HttpMethod.Get};
                    if (!_autoroutes.ContainsKey(rkey))
                    {
                        _autoroutes.Add(rkey,new RouteParameters() { EntityType = givenType, ItemsPerPage = r.ItemsPerPage});
                        LogInformation(String.Format("Add route {0} for {1}", rkey, givenType));
                        AddRoutesForProperties(givenType, routeName, r.ItemsPerPage);
                    }
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
        public void GetAutoControllers( string routePrefix, DatabaseTypes databaseType, string connectionString)
        {
            _connectionString = connectionString;
            DatabaseType = databaseType;
            _routePrefix = routePrefix;
            ProcessType (typeof(T));
            PropertyInfo[] p = typeof(T).GetProperties();
            foreach (PropertyInfo t in p)
            {
                ProcessType (t.PropertyType);
            }
        }
    }
}