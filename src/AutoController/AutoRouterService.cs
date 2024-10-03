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
    private static readonly Dictionary<string, List<AuthorizeAttribute>> Restrictions =
                   [];
    private static readonly Dictionary<Type, List<EntityKeyDescribtion>> EntityKeys =
                   [];
    private static readonly Dictionary<string, IAutoControllerOptions> ApiOptions =
                   [];
    private static readonly Dictionary<Type, MapToControllerAttribute> ControllerNames =
                   [];
    private static readonly Type MapToControllerGetParamAttributeType = typeof(MapToControllerGetParamAttribute);
    private static readonly Type MapToControllerAttributeType = typeof(MapToControllerAttribute);
    private static readonly Type GetRestictionAttributeType = typeof(GetRestrictionAttribute);
    private static readonly Type PostRestictionAttributeType = typeof(PostRestrictionAttribute);
    private static readonly Type DeleteRestictionAttributeType = typeof(DeleteRestrictionAttribute);
    private static string _connectionString = string.Empty;
    private static MethodInfo? _dbContextBeforeSaveChangesMethod;
    private static Func<T>? _dbContextFactory;

    private static DbContextOptions<T>? _dbContextOptions = null;
    /// <summary>
    /// Database type for autocontroller
    /// </summary>
    private static DatabaseTypes DatabaseType { get; set; }
    #endregion
    /// <summary>
    /// The Dictionary with all used routes
    /// </summary>
    private readonly Dictionary<RouteKey, RouteParameters> _autoroutes = [];

    /// <summary>
    /// The Dictionary with all used routes
    /// </summary>
    public Dictionary<RouteKey, RouteParameters> Autoroutes
    {
        get => _autoroutes;
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
                AccessDeniedPath = _accessDeniedPath ?? string.Empty,
                AuthentificationPath = _authentificationPath ?? string.Empty,
                RoutePrefix = _routePrefix ?? string.Empty,
                DefaultGetAction = _defaultGetAction ?? string.Empty,
                DefaultGetCountAction = _defaultGetCountAction ?? string.Empty,
                DefaultPostAction = _defaultPostAction ?? string.Empty,
                DefaultDeleteAction = _defaultDeleteAction ?? string.Empty,
                DefaultUpdateAction = _defaultUpdateAction ?? string.Empty,
                DefaultFilterParameter = _defaultFilterParameter ?? string.Empty,
                DefaultPageParameter = _defaultPageParameter ?? string.Empty,
                DefaultItemsPerPageParameter = _defaultItemsPerPageParameter ?? string.Empty,
                DefaultSortParameter = _defaultSortParameter ?? string.Empty,
                DefaultSortDirectionParameter = _defaultSortDirectionParameter ?? string.Empty,
                InteractingType = _defaultInteractingType,
                JsonSerializerOptions = _jsonOptions
            };
        }
    }

    private string? _routePrefix;
    private string? _startRoutePath;
    private InteractingType? _defaultInteractingType;
    private JsonSerializerOptions? _jsonOptions;
    private ILogger? logger;

    private string? _authentificationPath;
    private string? _accessDeniedPath;
    private string? _defaultGetAction;
    private string? _defaultGetCountAction;
    private string? _defaultPostAction;
    private string? _defaultDeleteAction;
    private string? _defaultUpdateAction;
    private string? _defaultFilterParameter;
    private string? _defaultSortParameter;
    private string? _defaultSortDirectionParameter;
    private string? _defaultPageParameter;
    private string? _defaultItemsPerPageParameter;
    private Dictionary<string, RequestParamName>? _requestParams;
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
    private void AddGetRoutesForEntity(string controllerName, Type givenType, InteractingType interactingType, bool allowAnonimus)
    {
        string basePath = _startRoutePath + controllerName;
        string countPath = basePath + "/" + _defaultGetCountAction;
        string defaultPath = basePath + "/" + _defaultGetAction;
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
                                                                DatabaseType,
                                                             _connectionString,
                                                              interactingType,
                                                              allowAnonimus,
                                                              _authentificationPath,
                                                              _accessDeniedPath,
                                                              _requestParams,
                                                             _jsonOptions,
                                                             _dbContextFactory,
                                                             _dbContextOptions ])
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
                                                                DatabaseType,
                                                            _connectionString,
                                                            allowAnonimus,
                                                            _authentificationPath,
                                                            _accessDeniedPath,
                                                            _requestParams,
                                                            _dbContextFactory,
                                                            _dbContextOptions ])
            };
            _autoroutes.Add(rkeyCount, rParam);
            LogInformation(string.Format("Add route {0} for {1}", rkeyCount, givenType));
        }
    }
    private void AddPostRouteForEntity(string controllerName, Type givenType, InteractingType interactingType)
    {
        string basePath = _startRoutePath + controllerName;
        string defaultPath = basePath + "/" + _defaultPostAction;
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
                                                                          DatabaseType,
                                                                          _connectionString,
                                                                          interactingType,
                                                                          _authentificationPath,
                                                                          _accessDeniedPath,
                                                                          _jsonOptions,
                                                                          _dbContextBeforeSaveChangesMethod,
                                                                          _dbContextFactory,
                                                                          _dbContextOptions ])
            };
            _autoroutes.Add(rkeyDefault, rParam);
            LogInformation(string.Format("Add route {0} for {1}", rkeyDefault, givenType));
        }
    }
    private void AddDeleteRouteForEntity(string controllerName, Type givenType, InteractingType interactingType)
    {
        string basePath = _startRoutePath + controllerName;
        string defaultPath = basePath + "/" + _defaultDeleteAction;
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
                                                                          DatabaseType,
                                                                          _connectionString,
                                                                          interactingType,
                                                                          _authentificationPath,
                                                                          _accessDeniedPath,
                                                                          _jsonOptions,
                                                                          _dbContextBeforeSaveChangesMethod,
                                                                          _dbContextFactory,
                                                                          _dbContextOptions ])
            };
            _autoroutes.Add(rkeyDefault, rParam);
            LogInformation(string.Format("Add route {0} for {1}", rkeyDefault, givenType));
        }
    }
    private void AddUpdateRouteForEntity(string controllerName, Type givenType, InteractingType interactingType)
    {
        string basePath = _startRoutePath + controllerName;
        string defaultPath = basePath + "/" + _defaultUpdateAction;
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
                                                                          DatabaseType,
                                                                          _connectionString,
                                                                          interactingType,
                                                                          _authentificationPath,
                                                                          _accessDeniedPath,
                                                                          _jsonOptions,
                                                                          _dbContextBeforeSaveChangesMethod,
                                                                          _dbContextFactory,
                                                                          _dbContextOptions ])
            };
            _autoroutes.Add(rkeyDefault, rParam);
            LogInformation(string.Format("Add route {0} for {1}", rkeyDefault, givenType));
        }
    }
    private static void ProcessType(Type givenType)
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
    private void CreateRoutes()
    {
        foreach (var c in ControllerNames)
        {
            InteractingType usedInteractingType = _defaultInteractingType == null ? c.Value.InteractingType : (InteractingType)_defaultInteractingType;
            AddGetRoutesForEntity(c.Value.ControllerName, c.Key, usedInteractingType, c.Value.AllowAnonimus);
            AddPostRouteForEntity(c.Value.ControllerName, c.Key, usedInteractingType);
            AddDeleteRouteForEntity(c.Value.ControllerName, c.Key, usedInteractingType);
            AddUpdateRouteForEntity(c.Value.ControllerName, c.Key, usedInteractingType);
        }
    }
    // private static void RetriveEntityKeys(Type entityType)
    // {
    //     // if (givenType.IsClass)
    //     // {
    //     //     PropertyInfo[] p = givenType.GetProperties();
    //     //     foreach (PropertyInfo t in p)
    //     //     {
    //     //         if (t.GetCustomAttribute(KeyAttributeType) is KeyAttribute k)
    //     //         {
    //     //             EntityKeys.TryAdd(givenType, new EntityKeyDescribtion { Name = t.Name, KeyType = t.PropertyType });
    //     //         }
    //     //     }
    //     // }
    //     // if (givenType.IsGenericType)
    //     // {
    //     //     foreach (Type currentType in givenType.GetGenericArguments())
    //     //     {
    //     //         RetriveEntityKeys(currentType);
    //     //     }
    //     // }
    // }

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
        CreateRoutes();
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
        CreateRoutes();
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
    /// <summary>
    /// Sets static parameters for dbcontext
    /// </summary>
    /// <param name="connString">Connection string</param>
    /// <param name="databaseType">Database type for DBContext</param>
    /// <param name="DbContextBeforeSaveChangesMethod">Method of DbContext to execute it before save data</param>
    /// <param name="DbContextFactory">Custom DbContext factory</param>
    /// <param name="dbContextOptions">Custom DbContext options</param>
    public static void SetStaticParams(DatabaseTypes databaseType, string connString, MethodInfo? DbContextBeforeSaveChangesMethod, Func<T>? DbContextFactory, DbContextOptions<T>? dbContextOptions = null)
    {
        _connectionString = connString;
        DatabaseType = databaseType;
        _dbContextBeforeSaveChangesMethod = DbContextBeforeSaveChangesMethod;
        _dbContextFactory = DbContextFactory;
        _dbContextOptions = dbContextOptions;

        using var ctx = Handler.CreateContext<T>(_connectionString, DatabaseType, _dbContextFactory, _dbContextOptions);
        foreach (Type entityType in  ctx.Model.GetEntityTypes().Select(t => t.ClrType).ToList())
        {
            var eType = ctx.Model.FindEntityType(entityType);
            var keys = eType?.FindPrimaryKey()?.Properties.ToList() ?? [];
            if (keys.Count > 0)
            {
                List<EntityKeyDescribtion> entityKeyDescribtions = [];
                foreach (var key in keys)
                {
                    entityKeyDescribtions.Add(new EntityKeyDescribtion() { Name = key.Name, KeyType = key.ClrType});
                }
                EntityKeys.TryAdd(entityType, entityKeyDescribtions);
                ProcessType(entityType);
            }
        }        
    }
}
