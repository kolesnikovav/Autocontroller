using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;

namespace AutoController;
/// <summary>
/// Discribe request path with http request type
/// </summary>
public class RouteKey
{
    /// <summary>
    /// Request path
    /// </summary>
    public string Path { get; set; } =null!;
    /// <summary>
    /// Http request method
    /// Currently, GET and POST supported
    /// </summary>
    public HttpMethod HttpMethod { get; set; } = null!;
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
    public Type EntityType { get; set; } = null!;
    /// <summary>
    ///
    /// </summary>
    public uint ItemsPerPage { get; set; }
    /// <summary>
    /// handler for request
    /// </summary>
    public RequestDelegate Handler { get; set; } = null!;
}
/// <summary>
/// Service that handle requests for Entityes
/// </summary>

public class AutoRouterService<T> where T : DbContext, IDisposable
{
    #region static members
    private static readonly Dictionary<string, List<AuthorizeAttribute>> Restrictions =[];
    private static readonly Dictionary<Type, List<EntityKeyDescribtion>> EntityKeys =[];
    private static readonly Dictionary<string, IAutoControllerOptions> ApiOptions = [];
    private static readonly Dictionary<Type, MapToControllerAttribute> ControllerNames = [];
    private static readonly Type MapToControllerGetParamAttributeType = typeof(MapToControllerGetParamAttribute);
    private static readonly Type MapToControllerAttributeType = typeof(MapToControllerAttribute);
    private static readonly Type GetRestictionAttributeType = typeof(GetRestrictionAttribute);
    private static readonly Type PostRestictionAttributeType = typeof(PostRestrictionAttribute);
    private static readonly Type DeleteRestictionAttributeType = typeof(DeleteRestrictionAttribute);
    private static MethodInfo? _dbContextBeforeSaveChangesMethod;
    #endregion
    /// <summary>
    /// The Dictionary with all used routes
    /// </summary>
    private readonly Dictionary<RouteKey, RouteParameters> _autoroutes = [];

    internal static void AddEntityKey (Type type, List<EntityKeyDescribtion> entityKeyDescribtions)
    {
        EntityKeys.TryAdd(type, entityKeyDescribtions);
    }
    internal static List<EntityKeyDescribtion> GetEntityKeyDescribtions (Type entityType) 
    => EntityKeys.TryGetValue(entityType, out var entityKeyDescribtions) ? entityKeyDescribtions : new List<EntityKeyDescribtion>();

    /// <summary>
    /// The Dictionary with all used routes
    /// </summary>
    public Dictionary<RouteKey, RouteParameters> Autoroutes
    {
        get => _autoroutes;
    }
    private ILogger? logger;
    private void LogInformation(string message)
    {
        logger?.LogInformation(message);
    }

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
            MapToControllerGetParamAttribute? b = pInfo.GetCustomAttribute(MapToControllerGetParamAttributeType) as MapToControllerGetParamAttribute;
            if (b != null)
            {
                string r = string.IsNullOrWhiteSpace(b.ParamName) ? pInfo.Name : b.ParamName;
                string optional = b.Optional ? "?" : string.Empty;
                string route = routeClassName + "/{" + r + optional + "}";
                //"{controller}/{action}/{property?}"
                RouteKey rkey = new() { Path = route, HttpMethod = HttpMethod.Get };
                _autoroutes.Add(
                    rkey,
                    new RouteParameters() { EntityType = pInfo.PropertyType, ItemsPerPage = itemsPerPage }
                    );
                LogInformation(string.Format("Add route {0} for {1} type {2}", rkey, pInfo.Name, pInfo.PropertyType));

            }
            // if (pInfo.GetCustomAttribute(KeyAttributeType) is KeyAttribute k)
            // {
            //     EntityKeys.TryAdd(givenType, new EntityKeyDescribtion { Name = pInfo.Name, KeyType = pInfo.PropertyType });
            //     string r = string.IsNullOrWhiteSpace(b?.ParamName) ? pInfo.Name : b.ParamName;
            //     string route = routeClassName + "/{" + r + "}";
            //     //"{controller}/{action}/{property}"
            //     RouteKey rkey = new() { Path = route, HttpMethod = HttpMethod.Post };
            //     _autoroutes.Add(
            //         rkey,
            //         new RouteParameters() { EntityType = pInfo.PropertyType }
            //         );
            //     LogInformation(string.Format("Add route {0} for {1} type {2}", rkey, pInfo.Name, pInfo.PropertyType));
            // }
        }
    }
    private static void ProccessRestrictions(Type givenType, HttpMethod httpMethod)
    {
        string AKey = AccessHelper.GetAccessKey(givenType, null, httpMethod);
        var restrictionsGet = givenType.GetCustomAttributes(GetRestictionAttributeType);
        var restrictionsPost = givenType.GetCustomAttributes(PostRestictionAttributeType);
        var restrictionsDelete = givenType.GetCustomAttributes(DeleteRestictionAttributeType);
        if (restrictionsGet.Count() > 0)
        {
            if (!Restrictions.TryGetValue(AKey, out List<AuthorizeAttribute>? value))
            {
                Restrictions.Add(AKey, []);
                foreach (var r in restrictionsGet)
                {
                    Restrictions[AKey].Add((AuthorizeAttribute)r);
                }
            }
            else
            {
                foreach (var r in restrictionsGet)
                {
                    value.Add((AuthorizeAttribute)r);
                }
            }
        }
        else if (restrictionsPost.Any() && httpMethod == HttpMethod.Post)
        {
            if (!Restrictions.TryGetValue(AKey, out List<AuthorizeAttribute>? value))
            {
                Restrictions.Add(AKey, []);
                foreach (var r in restrictionsPost)
                {
                    Restrictions[AKey].Add((AuthorizeAttribute)r);
                }
            }
            else
            {
                foreach (var r in restrictionsPost)
                {
                    value.Add((AuthorizeAttribute)r);
                }
            }
        }
        else if (restrictionsDelete.Any() && httpMethod == HttpMethod.Delete)
        {
            if (!Restrictions.TryGetValue(AKey, out List<AuthorizeAttribute>? value))
            {
                Restrictions.Add(AKey, []);
                foreach (var r in restrictionsDelete)
                {
                    Restrictions[AKey].Add((AuthorizeAttribute)r);
                }
            }
            else
            {
                foreach (var r in restrictionsDelete)
                {
                    value.Add((AuthorizeAttribute)r);
                }
            }
        }
    }
    private void AddGetRoutesForEntity(IServiceProvider serviceProvider, string controllerName, Type givenType, IAutoControllerOptions options, bool allowAnonimus)
    {
        string basePath = $"{options.RoutePrefix}/{controllerName}";
        string countPath = $"{basePath}/{options.DefaultGetCountAction}";
        string defaultPath = $"{basePath}/{options.DefaultGetAction}";
        RouteKey rkeyDefault = new() { Path = defaultPath, HttpMethod = HttpMethod.Get };
        RouteKey rkeyCount = new() { Path = countPath, HttpMethod = HttpMethod.Get };
        if (!_autoroutes.ContainsKey(rkeyDefault))
        {
            RouteParameters rParam = new()
            {
                EntityType = givenType,
                Handler = Handler.GetRequestDelegate("GetHandler",
                                                        [typeof(T), givenType],
                                                        this,
                                                        [
                                                            Restrictions,
                                                            EntityKeys,
                                                            serviceProvider,
                                                            options,
                                                            allowAnonimus ])
            };
            _autoroutes.Add(rkeyDefault, rParam);
            LogInformation(string.Format("Add route {0} for {1}", rkeyDefault, givenType));
        }
        if (!_autoroutes.ContainsKey(rkeyCount))
        {
            RouteParameters rParam = new()
            {
                EntityType = givenType,
                Handler = Handler.GetRequestDelegate("GetCountOf",
                                                        [typeof(T), givenType],
                                                        this,
                                                        [
                                                            Restrictions,
                                                            EntityKeys,
                                                            serviceProvider,
                                                            options,
                                                            allowAnonimus ])
            };
            _autoroutes.Add(rkeyCount, rParam);
            LogInformation(string.Format("Add route {0} for {1}", rkeyCount, givenType));
        }
        // add route GET /api/<entity>/<primary keys>
        var primaryKeys = GetEntityKeyDescribtions(givenType);
        if (primaryKeys.Count > 0)
        {
            string routeEntityKey = basePath;
            foreach (var key in primaryKeys)
            {
                routeEntityKey += "/{" + key.Name+"?}";
            }
            RouteKey rkeyGetByKey = new() { Path = routeEntityKey, HttpMethod = HttpMethod.Get };
            if (!_autoroutes.ContainsKey(rkeyGetByKey))
            {
                RouteParameters rParam = new()
                {
                    EntityType = givenType,
                    Handler = Handler.GetRequestDelegate("GetHandler",
                                                            [typeof(T), givenType],
                                                            this,
                                                            [
                                                                Restrictions,
                                                                EntityKeys,
                                                                serviceProvider,
                                                                options,
                                                                allowAnonimus ])
                };
                _autoroutes.Add(rkeyGetByKey, rParam);
                LogInformation(string.Format("Add route {0} for {1}", rkeyGetByKey, givenType));
            }
        }
    }
    private void AddPostRouteForEntity(IServiceProvider serviceProvider, string controllerName, Type givenType, IAutoControllerOptions options)
    {
        string basePath = $"{options.RoutePrefix}/{controllerName}";
        string defaultPath = $"{basePath}/{options.DefaultPostAction}";

        RouteKey rkeyDefault = new() { Path = defaultPath, HttpMethod = HttpMethod.Post };
        if (!_autoroutes.ContainsKey(rkeyDefault))
        {
            RouteParameters rParam = new()
            {
                EntityType = givenType,
                Handler = Handler.GetRequestDelegate("PostHandler",
                                                        [typeof(T), givenType],
                                                        this,
                                                        [
                                                            false,
                                                            Restrictions,
                                                            serviceProvider,
                                                            options,
                                                            _dbContextBeforeSaveChangesMethod])
            };
            _autoroutes.Add(rkeyDefault, rParam);
            LogInformation(string.Format("Add route {0} for {1}", rkeyDefault, givenType));
        }
    }
    private void AddDeleteRouteForEntity(IServiceProvider serviceProvider, string controllerName, Type givenType, IAutoControllerOptions options)
    {
        string basePath = $"{options.RoutePrefix}/{controllerName}";
        string defaultPath = $"{basePath}/{options.DefaultDeleteAction}";

        RouteKey rkeyDefault = new() { Path = defaultPath, HttpMethod = HttpMethod.Delete };
        if (!_autoroutes.ContainsKey(rkeyDefault))
        {
            RouteParameters rParam = new()
            {
                EntityType = givenType,
                Handler = Handler.GetRequestDelegate("DeleteHandler",
                                                        [typeof(T), givenType],
                                                        this,
                                                        [
                                                            Restrictions,
                                                            EntityKeys,
                                                            serviceProvider,
                                                            options,
                                                            _dbContextBeforeSaveChangesMethod])
            };
            _autoroutes.Add(rkeyDefault, rParam);
            LogInformation(string.Format("Add route {0} for {1}", rkeyDefault, givenType));
        }
        //***
        // add route DELETE /api/<entity>/<primary keys>
        var primaryKeys = GetEntityKeyDescribtions(givenType);
        if (primaryKeys.Count > 0)
        {
            string routeEntityKey = basePath;
            foreach (var key in primaryKeys)
            {
                routeEntityKey += "/{" + key.Name+"?}";
            }
            RouteKey rkeyGetByKey = new() { Path = routeEntityKey, HttpMethod = HttpMethod.Delete };
            if (!_autoroutes.ContainsKey(rkeyGetByKey))
            {
                RouteParameters rParam = new()
                {
                    EntityType = givenType,
                    Handler = Handler.GetRequestDelegate("DeleteHandler",
                                                            [typeof(T), givenType],
                                                            this,
                                                            [
                                                                Restrictions,
                                                                EntityKeys,
                                                                serviceProvider,
                                                                options,
                                                                _dbContextBeforeSaveChangesMethod ])
                };
                _autoroutes.Add(rkeyGetByKey, rParam);
                LogInformation(string.Format("Add route {0} for {1}", rkeyGetByKey, givenType));
            }
        }        
    }
    private void AddUpdateRouteForEntity(IServiceProvider serviceProvider, string controllerName, Type givenType, IAutoControllerOptions options)
    {
        string basePath = $"{options.RoutePrefix}/{controllerName}";
        string defaultPath = $"{basePath}/{options.DefaultUpdateAction}";        

        RouteKey rkeyDefault = new() { Path = defaultPath, HttpMethod = HttpMethod.Put };
        if (!_autoroutes.ContainsKey(rkeyDefault))
        {
            RouteParameters rParam = new()
            {
                EntityType = givenType,
                Handler = Handler.GetRequestDelegate("PostHandler",
                                                        [typeof(T), givenType],
                                                        this,
                                                        [
                                                            false,
                                                            Restrictions,
                                                            serviceProvider,
                                                            options,
                                                            _dbContextBeforeSaveChangesMethod])
            };
            _autoroutes.Add(rkeyDefault, rParam);
            LogInformation(string.Format("Add route {0} for {1}", rkeyDefault, givenType));
        }
    }
    internal static void ProcessType(Type givenType)
    {
        if (givenType.IsClass)
        {
            var a = givenType.GetCustomAttribute(MapToControllerAttributeType);
            MapToControllerAttribute? r =(MapToControllerAttribute?)a;
            if (r != null)
            {
                ControllerNames.TryAdd(givenType, r);
                ProccessRestrictions(givenType, HttpMethod.Get);
                ProccessRestrictions(givenType, HttpMethod.Post);
                ProccessRestrictions(givenType, HttpMethod.Delete);
            }
        }
        if (givenType.IsGenericType)
        {
            foreach (Type currentType in givenType.GetGenericArguments())
            {
                ProcessType(currentType);
            }
        }
    }
    private void CreateRoutes(IAutoControllerOptions autoControllerOptions, IServiceProvider serviceProvider)
    {
        foreach (var c in ControllerNames)
        {
            AddGetRoutesForEntity(serviceProvider, c.Value.ControllerName, c.Key, autoControllerOptions , c.Value.AllowAnonimus);
            AddPostRouteForEntity(serviceProvider, c.Value.ControllerName, c.Key, autoControllerOptions);
            AddDeleteRouteForEntity(serviceProvider, c.Value.ControllerName, c.Key, autoControllerOptions);
            AddUpdateRouteForEntity(serviceProvider, c.Value.ControllerName, c.Key, autoControllerOptions);
        }
    }
    /// <summary>
    /// Using System.Reflection generates api controller for given type and properties
    /// By default, Controller name is the same as class name
    /// </summary>
    /// <param name="autoControllerOptions">Options for controller</param>
    /// <param name="serviceProvider">IServiceProvider for Service collection</param>
    public void GetAutoControllers(
        IAutoControllerOptions autoControllerOptions, IServiceProvider serviceProvider)
    {
        // found keys of entity for filering results
        ApiOptions.TryAdd(autoControllerOptions.RoutePrefix, autoControllerOptions);
        CreateRoutes(autoControllerOptions,serviceProvider);
    }
    /// <summary>
    /// Retrive request response schema assosiated with api route prefix
    /// </summary>
    /// <param name="prefix">Route prefix</param>
    public static IAutoControllerOptions? GetOptions(string prefix)
    {
        if (!ApiOptions.TryGetValue(prefix, out IAutoControllerOptions? value)) return null;
        return value;
    }
}
