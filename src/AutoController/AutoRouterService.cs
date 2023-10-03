using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
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
    /// <summary>
    /// Equality checker for RouteKey
    /// </summary>
    /// <param name="obj">anover route key</param>
    /// <returns></returns>
    public bool Equals(RouteKey obj)
    {
        return HttpMethod.Equals(obj.HttpMethod) && Path.Equals(obj.Path);
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

public class AutoRouterService<T> where T : DbContext, IDisposable
{
    #region static members
    private static readonly Dictionary<string, List<AuthorizeAttribute>> Restrictions = new();
    private static readonly Dictionary<Type, EntityKeyDescribtion> EntityKeys = new();
    private static readonly Dictionary<string, IAutoControllerOptions> ApiOptions = new();
    private static readonly Dictionary<Type, MapToControllerAttribute> ControllerNames = new();
    private static readonly Type MapToControllerGetParamAttributeType = typeof(MapToControllerGetParamAttribute);
    private static readonly Type MapToControllerAttributeType = typeof(MapToControllerAttribute);
    private static readonly Type GetRestictionAttributeType = typeof(GetRestrictionAttribute);
    private static readonly Type PostRestictionAttributeType = typeof(PostRestrictionAttribute);
    private static readonly Type DeleteRestictionAttributeType = typeof(DeleteRestrictionAttribute);
    private static readonly Type KeyAttributeType = typeof(KeyAttribute);
    private static string _connectionString;
    private static MethodInfo _dbContextBeforeSaveChangesMethod;
    private static Func<T> _dbContextFactory;
    /// <summary>
    /// Database type for autocontroller
    /// </summary>
    private static DatabaseTypes DatabaseType { get; set; }
    #endregion
    /// <summary>
    /// The Dictionary with all used routes
    /// </summary>
    private readonly Dictionary<string,Dictionary<RouteKey, RouteParameters>> _autoroutes = new();
    /// <summary>
    /// The Dictionary with all used routes
    /// first key - api prefix
    /// </summary>
    public Dictionary<string,Dictionary<RouteKey, RouteParameters>> Autoroutes
    {
        get => _autoroutes;
    }
    /// <summary>
    /// GetAutoroutes by api prefix
    /// </summary>
    /// <param name="api_prefix"></param>
    /// <returns></returns> 
    /// <summary>
    public Dictionary<RouteKey, RouteParameters>? GetAutoroutes(string api_prefix)
    {
        return _autoroutes[api_prefix] ?? null;
    }


    /// <summary>
    /// Returns current options for external use
    /// </summary>
    public IAutoControllerOptions Options
    {
        get
        {
            return new AutoControllerOptions()
            {
                AccessDeniedPath = _accessDeniedPath,
                AuthentificationPath = _authentificationPath,
                RoutePrefix = _routePrefix,
                DefaultGetAction = _defaultGetAction,
                DefaultGetCountAction = _defaultGetCountAction,
                DefaultPostAction = _defaultPostAction,
                DefaultDeleteAction = _defaultDeleteAction,
                DefaultUpdateAction = _defaultUpdateAction,
                DefaultFilterParameter = _defaultFilterParameter,
                DefaultPageParameter = _defaultPageParameter,
                DefaultItemsPerPageParameter = _defaultItemsPerPageParameter,
                DefaultSortParameter = _defaultSortParameter,
                DefaultSortDirectionParameter = _defaultSortDirectionParameter,
                InteractingType = _defaultInteractingType,
                JsonSerializerOptions = _jsonOptions
            };
        }
    }

    private string _routePrefix;
    private string _startRoutePath;
    private InteractingType? _defaultInteractingType;
    private JsonSerializerOptions? _jsonOptions;
    private ILogger logger;

    private string _authentificationPath;
    private string _accessDeniedPath;
    private string _defaultGetAction;
    private string _defaultGetCountAction;
    private string _defaultPostAction;
    private string _defaultDeleteAction;
    private string _defaultUpdateAction;
    private string _defaultFilterParameter;
    private string _defaultSortParameter;
    private string _defaultSortDirectionParameter;
    private string _defaultPageParameter;
    private string _defaultItemsPerPageParameter;
    private Dictionary<string, RequestParamName> _requestParams;
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
            MapToControllerGetParamAttribute b = pInfo.GetCustomAttribute(MapToControllerGetParamAttributeType) as MapToControllerGetParamAttribute;
            if (b != null)
            {
                string r = string.IsNullOrWhiteSpace(b.ParamName) ? pInfo.Name : b.ParamName;
                string optional = b.Optional ? "?" : string.Empty;
                string route = routeClassName + "/{" + r + optional + "}";
                //"{controller}/{action}/{property?}"
                RouteKey rkey = new() { Path = route, HttpMethod = HttpMethod.Get };

                var api_route = new Dictionary<RouteKey,RouteParameters>(){rkey,new RouteParameters() { EntityType = pInfo.PropertyType, ItemsPerPage = itemsPerPage }};



                _autoroutes.Add(
                    rkey,
                    new RouteParameters() { EntityType = pInfo.PropertyType, ItemsPerPage = itemsPerPage }
                    );
                LogInformation(string.Format("Add route {0} for {1} type {2}", rkey, pInfo.Name, pInfo.PropertyType));

            }
            if (pInfo.GetCustomAttribute(KeyAttributeType) is KeyAttribute k)
            {
                EntityKeys.TryAdd(givenType, new EntityKeyDescribtion { Name = pInfo.Name, KeyType = pInfo.PropertyType });
                string r = string.IsNullOrWhiteSpace(b.ParamName) ? pInfo.Name : b.ParamName;
                string route = routeClassName + "/{" + r + "}";
                //"{controller}/{action}/{property}"
                RouteKey rkey = new() { Path = route, HttpMethod = HttpMethod.Post };
                _autoroutes.Add(
                    rkey,
                    new RouteParameters() { EntityType = pInfo.PropertyType }
                    );
                LogInformation(string.Format("Add route {0} for {1} type {2}", rkey, pInfo.Name, pInfo.PropertyType));
            }
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
            if (!Restrictions.ContainsKey(AKey))
            {
                Restrictions.Add(AKey, new List<AuthorizeAttribute>());
                foreach (var r in restrictionsGet)
                {
                    Restrictions[AKey].Add((AuthorizeAttribute)r);
                }
            }
            else
            {
                foreach (var r in restrictionsGet)
                {
                    Restrictions[AKey].Add((AuthorizeAttribute)r);
                }
            }
        }
        else if (restrictionsPost.Count() > 0 && httpMethod == HttpMethod.Post)
        {
            if (!Restrictions.ContainsKey(AKey))
            {
                Restrictions.Add(AKey, new List<AuthorizeAttribute>());
                foreach (var r in restrictionsPost)
                {
                    Restrictions[AKey].Add((AuthorizeAttribute)r);
                }
            }
            else
            {
                foreach (var r in restrictionsPost)
                {
                    Restrictions[AKey].Add((AuthorizeAttribute)r);
                }
            }
        }
        else if (restrictionsDelete.Count() > 0 && httpMethod == HttpMethod.Delete)
        {
            if (!Restrictions.ContainsKey(AKey))
            {
                Restrictions.Add(AKey, new List<AuthorizeAttribute>());
                foreach (var r in restrictionsDelete)
                {
                    Restrictions[AKey].Add((AuthorizeAttribute)r);
                }
            }
            else
            {
                foreach (var r in restrictionsDelete)
                {
                    Restrictions[AKey].Add((AuthorizeAttribute)r);
                }
            }
        }
    }
    private void AddGetRoutesForEntity(string api_prefix, string controllerName, Type givenType, InteractingType interactingType, bool allowAnonimus)
    {
        string basePath = _startRoutePath + controllerName;
        string countPath = basePath + "/" + _defaultGetCountAction;
        string defaultPath = basePath + "/" + _defaultGetAction;
        var apiRoutes = GetAutoroutes(api_prefix);
        if (apiRoutes == null)
        {
            apiRoutes = new Dictionary<RouteKey, RouteParameters>();
            _autoroutes.Add(api_prefix, apiRoutes);
        }        
        RouteKey rkeyDefault = new() { Path = defaultPath, HttpMethod = HttpMethod.Get };
        RouteKey rkeyCount = new() { Path = countPath, HttpMethod = HttpMethod.Get };
        if (!apiRoutes.ContainsKey(rkeyDefault))
        {
            RouteParameters rParam = new()
            {
                EntityType = givenType,
                Handler = Handler.GetRequestDelegate("GetHandler",
                                                        new Type[] { typeof(T), givenType },
                                                        this,
                                                        new object[] {
                                                                Restrictions,
                                                                EntityKeys,
                                                                DatabaseType,
                                                             _connectionString,
                                                              interactingType,
                                                              allowAnonimus,
                                                              _authentificationPath,
                                                              _accessDeniedPath,
                                                              _requestParams,
                                                             _jsonOptions,
                                                             _dbContextFactory })
            };
            apiRoutes.Add(rkeyDefault, rParam);
            LogInformation(string.Format("Add route {0} for {1}", rkeyDefault, givenType));
        }
        if (!apiRoutes.ContainsKey(rkeyCount))
        {
            RouteParameters rParam = new()
            {
                EntityType = givenType,
                Handler = Handler.GetRequestDelegate("GetCountOf",
                                                        new Type[] { typeof(T), givenType },
                                                        this,
                                                        new object[] {
                                                                Restrictions,
                                                                EntityKeys,
                                                                DatabaseType,
                                                            _connectionString,
                                                            allowAnonimus,
                                                            _authentificationPath,
                                                            _accessDeniedPath,
                                                            _requestParams,
                                                            _dbContextFactory })
            };
            apiRoutes.Add(rkeyCount, rParam);
            LogInformation(string.Format("Add route {0} for {1}", rkeyCount, givenType));
        }
    }
    private void AddPostRouteForEntity(string api_prefix,string controllerName, Type givenType, InteractingType interactingType)
    {
        string basePath = _startRoutePath + controllerName;
        string defaultPath = basePath + "/" + _defaultPostAction;
        var apiRoutes = GetAutoroutes(api_prefix);
        if (apiRoutes == null)
        {
            apiRoutes = new Dictionary<RouteKey, RouteParameters>();
            _autoroutes.Add(api_prefix, apiRoutes);
        }        
        RouteKey rkeyDefault = new() { Path = defaultPath, HttpMethod = HttpMethod.Post };
        if (!apiRoutes.ContainsKey(rkeyDefault))
        {
            RouteParameters rParam = new()
            {
                EntityType = givenType,
                Handler = Handler.GetRequestDelegate("PostHandler",
                                                        new Type[] { typeof(T), givenType },
                                                        this,
                                                        new object[] {
                                                                          false,
                                                                          Restrictions,
                                                                          DatabaseType,
                                                                          _connectionString,
                                                                          interactingType,
                                                                          _authentificationPath,
                                                                          _accessDeniedPath,
                                                                          _jsonOptions,
                                                                          _dbContextBeforeSaveChangesMethod,
                                                                          _dbContextFactory })
            };
            apiRoutes.Add(rkeyDefault, rParam);
            LogInformation(string.Format("Add route {0} for {1}", rkeyDefault, givenType));
        }
    }
    private void AddDeleteRouteForEntity(string api_prefix, string controllerName, Type givenType, InteractingType interactingType)
    {
        string basePath = _startRoutePath + controllerName;
        string defaultPath = basePath + "/" + _defaultDeleteAction;
        var apiRoutes = GetAutoroutes(api_prefix);
        if (apiRoutes == null)
        {
            apiRoutes = new Dictionary<RouteKey, RouteParameters>();
            _autoroutes.Add(api_prefix, apiRoutes);
        }        
        RouteKey rkeyDefault = new() { Path = defaultPath, HttpMethod = HttpMethod.Delete };
        if (!apiRoutes.ContainsKey(rkeyDefault))
        {
            RouteParameters rParam = new()
            {
                EntityType = givenType,
                Handler = Handler.GetRequestDelegate("DeleteHandler",
                                                        new Type[] { typeof(T), givenType },
                                                        this,
                                                        new object[] {
                                                                          Restrictions,
                                                                          DatabaseType,
                                                                          _connectionString,
                                                                          interactingType,
                                                                          _authentificationPath,
                                                                          _accessDeniedPath,
                                                                          _jsonOptions,
                                                                          _dbContextBeforeSaveChangesMethod,
                                                                          _dbContextFactory })
            };
            apiRoutes.Add(rkeyDefault, rParam);
            LogInformation(string.Format("Add route {0} for {1}", rkeyDefault, givenType));
        }
    }
    private void AddUpdateRouteForEntity(string api_prefix, string controllerName, Type givenType, InteractingType interactingType)
    {
        string basePath = _startRoutePath + controllerName;
        string defaultPath = basePath + "/" + _defaultUpdateAction;
        var apiRoutes = GetAutoroutes(api_prefix);
        if (apiRoutes == null)
        {
            apiRoutes = new Dictionary<RouteKey, RouteParameters>();
            _autoroutes.Add(api_prefix, apiRoutes);
        }

        RouteKey rkeyDefault = new() { Path = defaultPath, HttpMethod = HttpMethod.Put };
        if (!apiRoutes.ContainsKey(rkeyDefault))
        {
            RouteParameters rParam = new()
            {
                EntityType = givenType,
                Handler = Handler.GetRequestDelegate("PostHandler",
                                                        new Type[] { typeof(T), givenType },
                                                        this,
                                                        new object[] {
                                                                          false,
                                                                          Restrictions,
                                                                          DatabaseType,
                                                                          _connectionString,
                                                                          interactingType,
                                                                          _authentificationPath,
                                                                          _accessDeniedPath,
                                                                          _jsonOptions,
                                                                          _dbContextBeforeSaveChangesMethod,
                                                                          _dbContextFactory })
            };
            apiRoutes.Add(rkeyDefault, rParam);
            LogInformation(string.Format("Add route {0} for {1}", rkeyDefault, givenType));
        }
    }
    private static void ProcessType(Type givenType)
    {
        if (givenType.IsClass)
        {
            MapToControllerAttribute? r = (MapToControllerAttribute)givenType?.GetCustomAttribute(MapToControllerAttributeType);
            if (r != null)
            {
                if (!ControllerNames.ContainsKey(givenType))
                {
                    ControllerNames.Add(givenType, r);
                }
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
    private void CreateRoutes(string api_prefix)
    {
        foreach (var c in ControllerNames)
        {
            InteractingType usedInteractingType = _defaultInteractingType == null ? c.Value.InteractingType : (InteractingType)_defaultInteractingType;
            AddGetRoutesForEntity(api_prefix, c.Value.ControllerName, c.Key, usedInteractingType, c.Value.AllowAnonimus);
            AddPostRouteForEntity(api_prefix, c.Value.ControllerName, c.Key, usedInteractingType);
            AddDeleteRouteForEntity(api_prefix, c.Value.ControllerName, c.Key, usedInteractingType);
            AddUpdateRouteForEntity(api_prefix, c.Value.ControllerName, c.Key, usedInteractingType);
        }
    }
    private static void RetriveEntityKeys(Type givenType)
    {
        if (givenType.IsClass)
        {
            PropertyInfo[] p = givenType.GetProperties();
            foreach (PropertyInfo t in p)
            {
                if (t.GetCustomAttribute(KeyAttributeType) is KeyAttribute k)
                {
                    EntityKeys.TryAdd(givenType, new EntityKeyDescribtion { Name = t.Name, KeyType = t.PropertyType });
                }
            }
        }
        if (givenType.IsGenericType)
        {
            foreach (Type currentType in givenType.GetGenericArguments())
            {
                RetriveEntityKeys(currentType);
            }
        }
    }

    /// <summary>
    /// Use version without database type and connection string!!!
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
    /// <param name="DefaultGetCountAction">Sets the Route path for GetCount action. Default = Count</param>
    /// <param name="DefaultPostAction">Sets the the Route path for Save data via POST Request. Default = Save</param>
    /// <param name="DefaultDeleteAction">Sets the the Route path for Delete items via DELETE Request. Default = Delete</param>
    /// <param name="DefaultFilterParameter">Sets the parameter name wich describe filter expression. Default = filter</param>
    /// <param name="DefaultSortParameter">Sets the parameter name wich describe field to sort result. Default = sort</param>
    /// <param name="DefaultSortDirectionParameter">Sets the parameter name wich describe sortdirection. Default = sortdirection</param>
    /// <param name="DefaultPageParameter">Sets the parameter name of page number. Default = page</param>
    /// <param name="DefaultItemsPerPageParameter">Sets the parameter name of page size. Default = size</param>
    /// <param name="DefaultUpdateAction">Sets the the Route path for Update data via PUT Request. Default = Update</param>
    [Obsolete("Use version without database type & connection string")]
    public void GetAutoControllers(
        string routePrefix,
        DatabaseTypes databaseType,
        string connectionString,
        InteractingType? interactingType,
        string authentificationPath,
        string accessDeniedPath,
        JsonSerializerOptions? jsonSerializerOptions = null,
        string DefaultGetAction = "Index",
        string DefaultGetCountAction = "Count",
        string DefaultPostAction = "Save",
        string DefaultDeleteAction = "Delete",
        string DefaultFilterParameter = "filter",
        string DefaultSortParameter = "sort",
        string DefaultSortDirectionParameter = "sortdirection",
        string DefaultPageParameter = "page",
        string DefaultItemsPerPageParameter = "size",
        string DefaultUpdateAction = "Update")
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
        _defaultDeleteAction = DefaultDeleteAction;
        _defaultUpdateAction = DefaultUpdateAction;
        _defaultFilterParameter = DefaultFilterParameter;
        _defaultSortParameter = DefaultSortParameter;
        _defaultSortDirectionParameter = DefaultSortDirectionParameter;
        _defaultPageParameter = DefaultPageParameter;
        _defaultItemsPerPageParameter = DefaultItemsPerPageParameter;
        _startRoutePath = string.IsNullOrWhiteSpace(_routePrefix) ? string.Empty : _routePrefix + "/";
        _requestParams = RequestParams.Create(
            _defaultPageParameter,
            _defaultItemsPerPageParameter,
            _defaultFilterParameter,
            _defaultSortParameter,
            _defaultSortDirectionParameter
        );
        // found keys of entity for filering results
        if (!ApiOptions.ContainsKey(routePrefix))
        {
            ApiOptions.Add(routePrefix, new AutoControllerOptions
            {
                RoutePrefix = routePrefix,
                DefaultGetAction = _defaultGetAction,
                DefaultGetCountAction = _defaultGetCountAction,
                DefaultFilterParameter = _defaultFilterParameter,
                DefaultSortParameter = _defaultSortParameter,
                DefaultSortDirectionParameter = _defaultSortDirectionParameter,
                DefaultPageParameter = _defaultPageParameter,
                DefaultItemsPerPageParameter = _defaultItemsPerPageParameter,
                DefaultPostAction = _defaultPostAction,
                DefaultUpdateAction = _defaultUpdateAction,
                DefaultDeleteAction = _defaultDeleteAction,
                DatabaseType = DatabaseType,
                ConnectionString = _connectionString,
                InteractingType = _defaultInteractingType,
                JsonSerializerOptions = _jsonOptions,
                AuthentificationPath = _authentificationPath,
                AccessDeniedPath = _accessDeniedPath
            });
        }
        CreateRoutes(_routePrefix);
    }
    /// <summary>
    /// Create routes for each marked entity type
    /// </summary>
    /// <param name="routePrefix">Prefix segment for controller</param>
    /// <param name="interactingType">Designates interacting type with autocontroller. If null, interacting type of entity will be applied</param>
    /// <param name="DefaultGetAction">Sets the JsonSerializerOptions</param>
    /// <param name="jsonSerializerOptions">JsonSerializerOptions that will be applied during interacting</param>
    /// <param name="authentificationPath">Autentification page path</param>
    /// <param name="accessDeniedPath">Access denied page path</param>
    /// <param name="DefaultGetCountAction">Sets the Route path for GetCount action. Default = Count</param>
    /// <param name="DefaultPostAction">Sets the the Route path for Save data via POST Request. Default = Save</param>
    /// <param name="DefaultDeleteAction">Sets the the Route path for Delete items via DELETE Request. Default = Delete</param>
    /// <param name="DefaultFilterParameter">Sets the parameter name wich describe filter expression. Default = filter</param>
    /// <param name="DefaultSortParameter">Sets the parameter name wich describe field to sort result. Default = sort</param>
    /// <param name="DefaultSortDirectionParameter">Sets the parameter name wich describe sortdirection. Default = sortdirection</param>
    /// <param name="DefaultPageParameter">Sets the parameter name of page number. Default = page</param>
    /// <param name="DefaultItemsPerPageParameter">Sets the parameter name of page size. Default = size</param>
    /// <param name="DefaultUpdateAction">Sets the the Route path for Update data via PUT Request. Default = Update</param>
    public void GetAutoControllers(
        string routePrefix,
        InteractingType? interactingType,
        string authentificationPath,
        string accessDeniedPath,
        JsonSerializerOptions? jsonSerializerOptions = null,
        string DefaultGetAction = "Index",
        string DefaultGetCountAction = "Count",
        string DefaultPostAction = "Save",
        string DefaultDeleteAction = "Delete",
        string DefaultFilterParameter = "filter",
        string DefaultSortParameter = "sort",
        string DefaultSortDirectionParameter = "sortdirection",
        string DefaultPageParameter = "page",
        string DefaultItemsPerPageParameter = "size",
        string DefaultUpdateAction = "Update")
    {
        _routePrefix = routePrefix;
        _defaultInteractingType = interactingType;
        _authentificationPath = authentificationPath;
        _accessDeniedPath = accessDeniedPath;
        _jsonOptions = jsonSerializerOptions;
        _defaultGetAction = DefaultGetAction;
        _defaultGetCountAction = DefaultGetCountAction;
        _defaultPostAction = DefaultPostAction;
        _defaultDeleteAction = DefaultDeleteAction;
        _defaultUpdateAction = DefaultUpdateAction;
        _defaultFilterParameter = DefaultFilterParameter;
        _defaultSortParameter = DefaultSortParameter;
        _defaultSortDirectionParameter = DefaultSortDirectionParameter;
        _defaultPageParameter = DefaultPageParameter;
        _defaultItemsPerPageParameter = DefaultItemsPerPageParameter;
        _startRoutePath = string.IsNullOrWhiteSpace(_routePrefix) ? string.Empty : _routePrefix + "/";
        _requestParams = RequestParams.Create(
            _defaultPageParameter,
            _defaultItemsPerPageParameter,
            _defaultFilterParameter,
            _defaultSortParameter,
            _defaultSortDirectionParameter
        );
        // found keys of entity for filering results
        if (!ApiOptions.ContainsKey(routePrefix))
        {
            ApiOptions.Add(routePrefix, new AutoControllerOptions
            {
                RoutePrefix = routePrefix,
                DefaultGetAction = _defaultGetAction,
                DefaultGetCountAction = _defaultGetCountAction,
                DefaultFilterParameter = _defaultFilterParameter,
                DefaultSortParameter = _defaultSortParameter,
                DefaultSortDirectionParameter = _defaultSortDirectionParameter,
                DefaultPageParameter = _defaultPageParameter,
                DefaultItemsPerPageParameter = _defaultItemsPerPageParameter,
                DefaultPostAction = _defaultPostAction,
                DefaultUpdateAction = _defaultUpdateAction,
                DefaultDeleteAction = _defaultDeleteAction,
                DatabaseType = DatabaseType,
                ConnectionString = _connectionString,
                InteractingType = _defaultInteractingType,
                JsonSerializerOptions = _jsonOptions,
                AuthentificationPath = _authentificationPath,
                AccessDeniedPath = _accessDeniedPath
            });
        }
        CreateRoutes(_routePrefix);
    }
    /// <summary>
    /// Retrive request response schema assosiated with api route prefix
    /// </summary>
    /// <param name="prefix">Route prefix</param>
    public static IAutoControllerOptions? GetOptions(string prefix)
    {
        if (!ApiOptions.ContainsKey(prefix)) return null;
        return ApiOptions[prefix];
    }
    /// <summary>
    /// Sets static parameters for dbcontext
    /// </summary>
    /// <param name="connString">Connection string</param>
    /// <param name="databaseType">Database type for DBContext</param>
    /// <param name="DbContextBeforeSaveChangesMethod">Method of DbContext to execute it before save data</param>
    /// <param name="DbContextFactory">Custom DbContext factory</param>
    public static void SetStaticParams(DatabaseTypes databaseType, string connString, MethodInfo DbContextBeforeSaveChangesMethod, Func<T> DbContextFactory)
    {
        _connectionString = connString;
        DatabaseType = databaseType;
        _dbContextBeforeSaveChangesMethod = DbContextBeforeSaveChangesMethod;
        _dbContextFactory = DbContextFactory;
    }
    static AutoRouterService()
    {
        PropertyInfo[] p = typeof(T).GetProperties();
        foreach (PropertyInfo t in p)
        {
            RetriveEntityKeys(t.PropertyType);
            ProcessType(t.PropertyType);
        }
    }
}
