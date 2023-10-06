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
    public string Path { get; set; } = null!;
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
    private static string? _connectionString;
    private static MethodInfo _dbContextBeforeSaveChangesMethod;
    private static Func<T> _dbContextFactory;
    /// <summary>
    /// Database type for autocontroller
    /// </summary>
    private static DatabaseTypes DatabaseType { get; set; }
    #endregion

    /// <summary>
    /// options that depends on api prefix
    /// </summary>    
    private readonly Dictionary<string,IAutoControllerOptions> _options = new();

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
        if (_autoroutes.ContainsKey(api_prefix)) return _autoroutes[api_prefix];
        return null;
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
    private void AddGetRoutesForEntity(IAutoControllerOptions autoControllerOptions, string controllerName, Type givenType, InteractingType interactingType, bool allowAnonimus)
    {
        string api_prefix = autoControllerOptions.RoutePrefix;
        string basePath = GetStartPoutePath(api_prefix) + controllerName;
        string countPath = basePath + "/" + autoControllerOptions.DefaultGetCountAction;
        string defaultPath = basePath + "/" + autoControllerOptions.DefaultGetAction;
        var apiRoutes = GetAutoroutes(api_prefix);
        if (apiRoutes == null)
        {
            apiRoutes = new Dictionary<RouteKey, RouteParameters>();
            _autoroutes.Add(api_prefix, apiRoutes);
        }        
        RouteKey rkeyDefault = new() { Path = defaultPath, HttpMethod = HttpMethod.Get };
        RouteKey rkeyCount = new() { Path = countPath, HttpMethod = HttpMethod.Get };
        var requestParams = RequestParams.Create(
            autoControllerOptions.DefaultPageParameter,
             "size",
              autoControllerOptions.DefaultFilterParameter,
              autoControllerOptions.DefaultSortParameter,
              autoControllerOptions.DefaultSortDirectionParameter
            );

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
                                                              autoControllerOptions.AuthentificationPath,
                                                              autoControllerOptions.AccessDeniedPath,
                                                              requestParams,
                                                             autoControllerOptions.JsonSerializerOptions,
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
                                                            autoControllerOptions.AuthentificationPath,
                                                            autoControllerOptions.AccessDeniedPath,
                                                            requestParams,
                                                            _dbContextFactory })
            };
            apiRoutes.Add(rkeyCount, rParam);
            LogInformation(string.Format("Add route {0} for {1}", rkeyCount, givenType));
        }
    }
    private void AddPostRouteForEntity(IAutoControllerOptions autoControllerOptions,string controllerName, Type givenType, InteractingType interactingType)
    {
        string api_prefix = autoControllerOptions.RoutePrefix;
        string basePath = GetStartPoutePath(api_prefix) + controllerName;        
        string defaultPath = basePath + "/" + autoControllerOptions.DefaultPostAction;
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
                                                                          autoControllerOptions.AuthentificationPath,
                                                                          autoControllerOptions.AccessDeniedPath,
                                                                          autoControllerOptions.JsonSerializerOptions,
                                                                          _dbContextBeforeSaveChangesMethod,
                                                                          _dbContextFactory })
            };
            apiRoutes.Add(rkeyDefault, rParam);
            LogInformation(string.Format("Add route {0} for {1}", rkeyDefault, givenType));
        }
    }
    private void AddDeleteRouteForEntity(IAutoControllerOptions autoControllerOptions, string controllerName, Type givenType, InteractingType interactingType)
    {
        string api_prefix = autoControllerOptions.RoutePrefix;
        string basePath = GetStartPoutePath(api_prefix) + controllerName;        
        string defaultPath = basePath + "/" + autoControllerOptions.DefaultDeleteAction;        
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
                                                                          autoControllerOptions.AuthentificationPath,
                                                                          autoControllerOptions.AccessDeniedPath,
                                                                          autoControllerOptions.JsonSerializerOptions,
                                                                          _dbContextBeforeSaveChangesMethod,
                                                                          _dbContextFactory })
            };
            apiRoutes.Add(rkeyDefault, rParam);
            LogInformation(string.Format("Add route {0} for {1}", rkeyDefault, givenType));
        }
    }
    private void AddUpdateRouteForEntity(IAutoControllerOptions autoControllerOptions, string controllerName, Type givenType, InteractingType interactingType)
    {
        string api_prefix = autoControllerOptions.RoutePrefix;
        string basePath = GetStartPoutePath(api_prefix) + controllerName;        
        string defaultPath = basePath + "/" + autoControllerOptions.DefaultUpdateAction;        
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
                                                                          autoControllerOptions.AuthentificationPath,
                                                                          autoControllerOptions.AccessDeniedPath,
                                                                          autoControllerOptions.JsonSerializerOptions,
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
            var rAttr = givenType?.GetCustomAttribute(MapToControllerAttributeType);
            MapToControllerAttribute? r = rAttr == null ? null : (MapToControllerAttribute)rAttr;
            if (r != null)
            {
                if (!ControllerNames.ContainsKey(givenType!))
                {
                    ControllerNames.Add(givenType!, r);
                }
                ProccessRestrictions(givenType!, HttpMethod.Get);
                ProccessRestrictions(givenType!, HttpMethod.Post);
                ProccessRestrictions(givenType!, HttpMethod.Delete);
            }
        }
        if (givenType!.IsGenericType)
        {
            foreach (Type currentType in givenType.GetGenericArguments())
            {
                ProcessType(currentType);
            }
        }
    }
    private string GetStartPoutePath(string prefix)
    => string.IsNullOrWhiteSpace(prefix) ? string.Empty : prefix + "/";

    private void CreateRoutes(string api_prefix)
    {
        IAutoControllerOptions options = GetControllerOptions(api_prefix) ?? throw (new Exception(string.Format("AutoController options with prefix {0} Not found", api_prefix)));
        foreach (var c in ControllerNames)
        {
            InteractingType usedInteractingType = options.InteractingType ?? InteractingType.JSON;
            AddGetRoutesForEntity(options, c.Value.ControllerName, c.Key, usedInteractingType, c.Value.AllowAnonimus);
            AddPostRouteForEntity(options, c.Value.ControllerName, c.Key, usedInteractingType);
            AddDeleteRouteForEntity(options, c.Value.ControllerName, c.Key, usedInteractingType);
            AddUpdateRouteForEntity(options, c.Value.ControllerName, c.Key, usedInteractingType);
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

        var options = GetControllerOptions(routePrefix);
        if (options == null)
        {
            options = new AutoControllerOptions
            {
                RoutePrefix = routePrefix
            };
            _options.Add(routePrefix, options);
        };
        options.RoutePrefix = routePrefix;
        options.InteractingType = interactingType;
        options.AuthentificationPath = authentificationPath;
        options.AccessDeniedPath = accessDeniedPath;
        options.JsonSerializerOptions =jsonSerializerOptions;
        options.DefaultGetAction =DefaultGetAction;
        options.DefaultGetCountAction =DefaultGetCountAction;
        options.DefaultPostAction =DefaultPostAction;
        options.DefaultDeleteAction =DefaultDeleteAction;
        options.DefaultFilterParameter =DefaultFilterParameter;
        options.DefaultSortParameter =DefaultSortParameter;
        options.DefaultSortDirectionParameter =DefaultSortDirectionParameter;
        options.DefaultPageParameter =DefaultPageParameter;
        options.DefaultItemsPerPageParameter =DefaultItemsPerPageParameter;
        options.DefaultSortParameter =DefaultSortParameter;
        options.DefaultUpdateAction =DefaultUpdateAction;
        CreateRoutes(routePrefix);
    }
    /// <summary>
    /// Get options for route api prefix
    /// </summary>
    /// <param name="routePrefix"></param>
    /// <returns></returns>Optons for api prefix <summary>
    /// 
    /// </summary>
    /// <returns></returns>

    public IAutoControllerOptions? GetControllerOptions(string routePrefix)
    => _options.ContainsKey(routePrefix) ? _options[routePrefix] : null;

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
        var options = GetControllerOptions(routePrefix);
        if (options == null)
        {
            options = new AutoControllerOptions
            {
                RoutePrefix = routePrefix
            };
            _options.Add(routePrefix, options);
        };
        options.RoutePrefix = routePrefix;
        options.InteractingType = interactingType;
        options.AuthentificationPath = authentificationPath;
        options.AccessDeniedPath = accessDeniedPath;
        options.JsonSerializerOptions =jsonSerializerOptions;
        options.DefaultGetAction =DefaultGetAction;
        options.DefaultGetCountAction =DefaultGetCountAction;
        options.DefaultPostAction =DefaultPostAction;
        options.DefaultDeleteAction =DefaultDeleteAction;
        options.DefaultFilterParameter =DefaultFilterParameter;
        options.DefaultSortParameter =DefaultSortParameter;
        options.DefaultSortDirectionParameter =DefaultSortDirectionParameter;
        options.DefaultPageParameter =DefaultPageParameter;
        options.DefaultItemsPerPageParameter =DefaultItemsPerPageParameter;
        options.DefaultSortParameter =DefaultSortParameter;
        options.DefaultUpdateAction =DefaultUpdateAction;
        CreateRoutes(routePrefix);
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
