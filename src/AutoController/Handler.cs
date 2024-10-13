using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;

using System.Xml.Serialization;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Org.BouncyCastle.Asn1.X509.Qualified;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ZstdSharp.Unsafe;

namespace AutoController;
internal static class Handler
{
    private static MethodInfo? GetActionBeforeSave<TE>() where TE : class
    {
        return typeof(TE).GetRuntimeMethod("DoBeforeSave", [typeof(DbContext), typeof(string)]);
    }
    private static MethodInfo? GetActionBeforeDelete<TE>() where TE : class
    {
        return typeof(TE).GetRuntimeMethod("DoBeforeDelete", [typeof(DbContext), typeof(string)]);
    }
    private static MemoryStream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
    private static List<EntityKeyDescribtion> GetEntityKeyDescribtions<T, TE>() 
    where T : DbContext, IDisposable
    where TE : class    
    {
        return AutoRouterService<T>.GetEntityKeyDescribtions(typeof(TE));
    }

    private static bool Authorization<TE>(HttpContext context,
                                            HttpMethod requestMethod,
                                            Dictionary<string, List<AuthorizeAttribute>> restrictions,
                                            bool allowAnonimus,
                                            string authentificationPath,
                                            string accessDeniedPath)
    {
        string AKey = AccessHelper.GetAccessKey(typeof(TE), null, requestMethod);
        if (allowAnonimus && context.Request.Method == HttpMethods.Get) return true;
        if (!restrictions.TryGetValue(AKey, out List<AuthorizeAttribute>? value)) return true; // no restrictions!
        var autentificated = context.User.Identity?.IsAuthenticated ?? true;
        if (!autentificated)
        {
            context.Response.Redirect(authentificationPath);
            if (!autentificated)
            {
                context.Response.Redirect(authentificationPath);
                return false;
            }
        }
        else // user is authentificated
        {
            return AccessHelper.EvaluateAccessRightsAsync(context, value);
        }
        return true;
    }
    private static IEnumerable<TE> GetDBQueryResultByRouteKeys<T, TE>(
        T dbcontext,
        Microsoft.AspNetCore.Routing.RouteValueDictionary QueryParams,
        List<EntityKeyDescribtion> entityKeyDescribtions) 
    where T : DbContext, IDisposable
    where TE : class
    {
        IEnumerable<TE> queryResult;
        List<Expression> expressions = [];
        Expression? expressionEqulityAll = null;
        ParameterExpression inputParam = Expression.Parameter(typeof(TE), "x");
        foreach(var RouteParameters in QueryParams)
        {
            var entityKeyDescribtion = entityKeyDescribtions.FirstOrDefault(x => x.Name == RouteParameters.Key);
            Expression c;
            Type typeKey = entityKeyDescribtion.KeyType;

            var routeParam = RouteParameters.Value as string ?? "";
            if (routeParam == null) continue;
            MemberInfo? memberInfo = typeof(TE).GetMember(entityKeyDescribtion.Name).FirstOrDefault();
            if (memberInfo == null) continue;
            Expression inputParamAccess = inputParam.MakeMemberAccess(memberInfo);

            if (typeKey == typeof(int))
            {
                var p = int.TryParse(routeParam, out int o);
                if (!p)
                {
                    throw new TypeAccessException($"Invalid int type conversion {routeParam}");
                }
                c = Expression.Constant(o, typeKey);
            } else if (typeKey == typeof(Guid))
            {
                var p = Guid.TryParse(routeParam, out Guid o);
                if (!p)
                {
                    throw new TypeAccessException($"Invalid Guid type conversion {routeParam}");
                }
                c = Expression.Constant(o, typeKey);                
            } else if (typeKey == typeof(uint))
            {
                var p = uint.TryParse(routeParam, out uint o);
                if (!p)
                {
                    throw new TypeAccessException($"Invalid uint type conversion {routeParam}");
                }
                c = Expression.Constant(o, typeKey); 
            } else if (typeKey == typeof(bool))
            {
                var p = bool.TryParse(routeParam, out bool o);
                if (!p)
                {
                    throw new TypeAccessException($"Invalid bool type conversion {routeParam}");
                }
                c = Expression.Constant(o, typeKey); 
            } else if (typeKey == typeof(DateOnly))
            {
                var p = DateOnly.TryParse(routeParam, out DateOnly o);
                if (!p)
                {
                    throw new TypeAccessException($"Invalid DateOnly type conversion {routeParam}");
                }
                c = Expression.Constant(o, typeKey); 
            } else if (typeKey == typeof(DateTime))
            {
                var p = DateTime.TryParse(routeParam, out DateTime o);
                if (!p)
                {
                    throw new TypeAccessException($"Invalid DateTime type conversion {routeParam}");
                }
                c = Expression.Constant(o, typeKey);                 
            } else 
            {
                c = Expression.Constant(routeParam, typeof(string));
            }
            Expression eqExpression = Expression.Equal(inputParamAccess, c);
            expressions.Add(eqExpression);
        }
        if (expressions.Count == 1) 
        {
            expressionEqulityAll = expressions[0];
        } else if (expressions.Count > 1)
        {
            
            Expression[] expressionsA = [];
            for (int i = 0; i < expressions.Count-2; i++)
            {
                var eq = Expression.AndAlso(expressions[i], expressions[i+1]);
                _ = expressionsA.Append(eq);
            }
            expressionEqulityAll = Expression.Block(expressionsA);
        }
        if (expressionEqulityAll != null)
        {
            Func<TE, bool> f= Expression.Lambda<Func<TE, bool>>(expressionEqulityAll, inputParam).Compile();
            if (f != null) 
            {
                queryResult = [.. dbcontext.Set<TE>().AsNoTracking().Where<TE>(f)];
                return queryResult;                  
            }
        }
        return [];
    }    
    private static IEnumerable<TE> GetDBQueryResult<T, TE>(T dbcontext, UserRequestParams QueryParams) 
    where T : DbContext, IDisposable
    where TE : class
    {
        IEnumerable<TE> queryResult;
        int skip = (int)((QueryParams.pageNumber - 1) * QueryParams.pageSize);
        if (QueryParams.pageSize == 0)
        {
            if (!string.IsNullOrWhiteSpace(QueryParams.sort) && typeof(TE).GetProperty(QueryParams.sort) != null)
            {
                if (!string.IsNullOrWhiteSpace(QueryParams.sortDirection))
                {
                    queryResult = [.. dbcontext.Set<TE>().AsNoTracking().Where(QueryParams.filter).OrderByDescending(QueryParams.sort)];
                }
                else
                {
                    queryResult = [.. dbcontext.Set<TE>().AsNoTracking().Where(QueryParams.filter).OrderBy(QueryParams.sort)];
                }
            }
            else
            {
                queryResult = [.. dbcontext.Set<TE>().AsNoTracking().Where(QueryParams.filter)];
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(QueryParams.sort) && typeof(TE).GetProperty(QueryParams.sort) != null)
            {
                if (!string.IsNullOrWhiteSpace(QueryParams.sortDirection))
                {
                    queryResult = [.. dbcontext.Set<TE>().AsNoTracking().Where(QueryParams.filter).OrderByDescending(QueryParams.sort).Skip(skip).Take((int)QueryParams.pageSize)];
                }
                else
                {
                    queryResult = [.. dbcontext.Set<TE>().AsNoTracking().Where(QueryParams.filter).OrderBy(QueryParams.sort).Skip(skip).Take((int)QueryParams.pageSize)];
                }
            }
            else
            {
                queryResult = [.. dbcontext.Set<TE>().AsNoTracking().Where(QueryParams.filter).Skip(skip).Take((int)QueryParams.pageSize)];
            }
        }
        return queryResult;
    }
    private static RequestDelegate GetHandler<T, TE>(
        Dictionary<string, List<AuthorizeAttribute>> restrictions,
        Dictionary<Type, List<EntityKeyDescribtion>> entityKeys,
        IServiceProvider serviceProvider,
        IAutoControllerOptions options,
        bool allowAnonimus) where T : DbContext, IDisposable
                            where TE : class
    {
        return async (context) =>
        {
            bool authResult = Authorization<TE>(context, HttpMethod.Get, restrictions, allowAnonimus, options.AuthentificationPath, options.AccessDeniedPath);
            if (!authResult)
            {
                return;
            }
            var QueryParams = RequestParams.RetriveQueryParam(context.Request.Query, options.RequestParamNames);
            var QueryParamsFromRoute = context.Request.RouteValues;
            using IServiceScope serviceProviderScoped = serviceProvider.CreateScope();
            using T dbcontext = serviceProviderScoped.ServiceProvider.GetService<T>()!;
            {
                IEnumerable<TE> queryResult;
                if (QueryParamsFromRoute.Count > 0)
                {
                    var keys = GetEntityKeyDescribtions<T, TE>();
                    queryResult = GetDBQueryResultByRouteKeys<T, TE>(dbcontext, QueryParamsFromRoute, keys);
                }
                else
                {
                   queryResult = GetDBQueryResult<T, TE>(dbcontext, QueryParams);
                } 
                if (options.InteractingType == InteractingType.JSON)
                {
                   byte[] jsonUtf8Bytes;
                   jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(queryResult, options.JsonSerializerOptions);
                   context.Response.Headers.ContentType="application/json";
                   context.Response.Headers.ContentEncoding = "utf-8";
                   await context.Response.WriteAsync(Encoding.UTF8.GetString(jsonUtf8Bytes));
                }
                else if (options.InteractingType == InteractingType.XML)
               {
                   XmlRootAttribute a = new("result");
                   // XmlSerializer does not support IEnumerable<T>
                   XmlSerializer serializer = new(typeof(List<TE>), a);
                   StringWriter textWriter = new();
                   serializer.Serialize(textWriter, queryResult.ToList());
                   context.Response.Headers.ContentType="application/xml";
                   context.Response.Headers.ContentEncoding = "utf-8";                   
                   await context.Response.WriteAsync(textWriter.ToString());
                   await textWriter.DisposeAsync();
               }                
            }
        };
    }
    private static RequestDelegate GetCountOf<T, TE>(Dictionary<string, List<AuthorizeAttribute>> restrictions,
                                                     Dictionary<Type, List<EntityKeyDescribtion>> entityKeys,
                                                     IServiceProvider serviceProvider,
                                                     IAutoControllerOptions options,
                                                     bool allowAnonimus) where T : DbContext, IDisposable
                                                                         where TE : class
    {
        return async (context) =>
        {
            bool authResult = Authorization<TE>(context, HttpMethod.Get, restrictions, allowAnonimus, options.AuthentificationPath, options.AccessDeniedPath);
            if (!authResult)
            {
                return;
            }
            var QueryParams = RequestParams.RetriveQueryParam(context.Request.Query, options.RequestParamNames);
            int queryResult;

            using IServiceScope serviceProviderScoped = serviceProvider.CreateScope();
            using T dbcontext = serviceProviderScoped.ServiceProvider.GetService<T>()!;            
            {
                queryResult = GetDBQueryResult<T, TE>(dbcontext, QueryParams).Count<TE>();
            }
            context.Response.Headers.ContentType="text/plain";
            context.Response.Headers.ContentEncoding = "utf-8";
            await context.Response.WriteAsync(queryResult.ToString());
        };
    }
    private static bool CheckAllowed<T, TE>(T dbcontext, TE recivedObject, MethodInfo? methodInfo, out string reason) 
    where T : DbContext, IDisposable
    where TE : class
    {
        reason = "";
        bool result = true;
        if (methodInfo == null) return result;
        object[] p = [dbcontext, reason];
        result = (bool?)methodInfo.Invoke(recivedObject, p) ?? true;
        reason += (string)p[1];
        return result;
    }
    private static bool CheckAllowedList<T, TE>(T dbcontext, List<TE> recivedObjects, MethodInfo? methodInfo, out string reason) 
    where T : DbContext, IDisposable
    where TE : class
    {
        reason = "";
        bool result = true;
        if (methodInfo == null) return result;
        object[] p = [dbcontext, reason];
        foreach (TE el in recivedObjects)
        {
            result = ((bool?)methodInfo.Invoke(el, p) ?? true) && result;
            reason += (string)p[1] + "\n";
        }
        return result;
    }
    private static void DoBeforeContextSaveChanges<T>(MethodInfo? method, T context, object[]? parameters = null) where T : DbContext, IDisposable
    {
        if (method == null) return;
        method.Invoke(context, parameters);
    }

    private static RequestDelegate PostHandler<T, TE>(
        bool update,
        Dictionary<string, List<AuthorizeAttribute>> restrictions,
        IServiceProvider serviceProvider,
        IAutoControllerOptions options,
        MethodInfo? dbContextBeforeSaveChangesMethod = null) where T : DbContext, IDisposable
                                                            where TE : class
    {
        return async (context) =>
        {
            bool authResult = Authorization<TE>(context, HttpMethod.Post, restrictions, false, options.AuthentificationPath, options.AccessDeniedPath);
            if (!authResult)
            {
                return;
            }
            TE? recivedData;
            List<TE>? recivedDataList;
            var mi = GetActionBeforeSave<TE>();
            string Reason = "";

            using IServiceScope serviceProviderScoped = serviceProvider.CreateScope();
            using T dbcontext = serviceProviderScoped.ServiceProvider.GetService<T>()!;             
            if (options.InteractingType == InteractingType.JSON)
            {
                using var reader = new StreamReader(context.Request.Body);
                var body = await reader.ReadToEndAsync();
                try
                {
                    recivedData = JsonSerializer.Deserialize<TE>(body);
                    if (recivedData != null)
                    {
                        if (CheckAllowed<T, TE>(dbcontext, recivedData, mi, out Reason))
                        {
                            if (!update)
                            {
                                dbcontext.Set<TE>().Add(recivedData);
                            }
                            else
                            {
                                dbcontext.Set<TE>().Update(recivedData);
                            }
                            DoBeforeContextSaveChanges<T>(dbContextBeforeSaveChangesMethod, dbcontext);
                            await dbcontext.SaveChangesAsync();
                            byte[] jsonUtf8Bytes;
                            jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(recivedData, options.JsonSerializerOptions);
                            context.Response.Headers.ContentType="application/json";
                            context.Response.Headers.ContentEncoding = "utf-8";                            
                            await context.Response.WriteAsync(Encoding.UTF8.GetString(jsonUtf8Bytes));
                        }
                        else
                        {
                            context.Response.Headers.ContentType="text/plain";
                            context.Response.Headers.ContentEncoding = "utf-8";                            
                            await context.Response.WriteAsync(Reason);
                        }
                    }
                }
                catch
                {
                    try
                    {
                        recivedDataList = JsonSerializer.Deserialize<List<TE>>(body);
                        if (recivedDataList != null)
                        {
                            if (!CheckAllowedList<T, TE>(dbcontext, recivedDataList, mi, out Reason))
                            {
                                await context.Response.WriteAsync(Reason);
                            }
                            else
                            {
                                if (!update)
                                {
                                    dbcontext.Set<TE>().AddRange(recivedDataList);
                                }
                                else
                                {
                                    dbcontext.Set<TE>().UpdateRange(recivedDataList);
                                }
                                DoBeforeContextSaveChanges<T>(dbContextBeforeSaveChangesMethod, dbcontext);
                                await dbcontext.SaveChangesAsync();
                                byte[] jsonUtf8Bytes;
                                jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(recivedDataList, options.JsonSerializerOptions);
                                context.Response.Headers.ContentType="application/json";
                                context.Response.Headers.ContentEncoding = "utf-8";                                  
                                await context.Response.WriteAsync(Encoding.UTF8.GetString(jsonUtf8Bytes));
                            }
                        }
                    }
                    catch
                    {

                    }
                }
            }
            else if (options.InteractingType == InteractingType.XML)
            {
                using var reader = new StreamReader(context.Request.Body);
                var body = await reader.ReadToEndAsync();

                Stream stream = GenerateStreamFromString(body);
                XmlRootAttribute a = new("result");
                XmlSerializer serializer = new(typeof(List<TE>), a);
                var recivedDataL = (List<TE>?)serializer.Deserialize(stream);
                await stream.DisposeAsync();
                if (recivedDataL != null && recivedDataL.Count > 0)
                {
                    if (CheckAllowedList<T, TE>(dbcontext, recivedDataL, mi, out Reason))
                    {
                        if (!update)
                        {
                            dbcontext.Set<TE>().AddRange(recivedDataL);
                        }
                        else
                        {
                            dbcontext.Set<TE>().UpdateRange(recivedDataL);
                        }
                        DoBeforeContextSaveChanges<T>(dbContextBeforeSaveChangesMethod, dbcontext);
                        await dbcontext.SaveChangesAsync();
                        StringWriter textWriter = new();
                        serializer.Serialize(textWriter, recivedDataL.ToList());
                        context.Response.Headers.ContentType="application/xlm";
                        context.Response.Headers.ContentEncoding = "utf-8";                    
                        await context.Response.WriteAsync(textWriter.ToString());
                        await textWriter.DisposeAsync();
                    }
                    else
                    {
                        await context.Response.WriteAsync(Reason);
                    }
                }
                // Do something
            }
        };
    }
    private static RequestDelegate DeleteHandler<T, TE>(
        Dictionary<string, List<AuthorizeAttribute>> restrictions,
        Dictionary<Type, List<EntityKeyDescribtion>> entityKeys,
        IServiceProvider serviceProvider,
        IAutoControllerOptions options,
        MethodInfo? dbContextBeforeSaveChangesMethod = null) where T : DbContext, IDisposable
                                                            where TE : class
    {
        return async (context) =>
        {
            bool authResult = Authorization<TE>(context, HttpMethod.Delete, restrictions, false, options.AuthentificationPath, options.AccessDeniedPath);
            if (!authResult)
            {
                return;
            }
            TE? recivedData;
            List<TE>? recivedDataList;
            var mi = GetActionBeforeDelete<TE>();
            string Reason = "";
            using IServiceScope serviceProviderScoped = serviceProvider.CreateScope();
            using T dbcontext = serviceProviderScoped.ServiceProvider.GetService<T>()!;
            var QueryParamsFromRoute = context.Request.RouteValues;
            if (QueryParamsFromRoute.Count > 0)
            {
                var keys = GetEntityKeyDescribtions<T, TE>();
                TE? queryResult = GetDBQueryResultByRouteKeys<T, TE>(dbcontext, QueryParamsFromRoute, keys).FirstOrDefault();
                if (queryResult != null)
                {
                    if (CheckAllowed<T, TE>(dbcontext, queryResult, mi, out Reason))
                    {
                        dbcontext.Set<TE>().Remove(queryResult);
                        DoBeforeContextSaveChanges<T>(dbContextBeforeSaveChangesMethod, dbcontext);
                        await dbcontext.SaveChangesAsync();
                        context.Response.Headers.ContentType = "text/plain";
                        context.Response.Headers.ContentEncoding = "utf-8";
                        await context.Response.WriteAsync("Deleted");
                    }
                    else
                    {
                        await context.Response.WriteAsync(Reason);
                    }
                }
            } else if (options.InteractingType == InteractingType.JSON)
            {
                using var reader = new StreamReader(context.Request.Body);
                var body = await reader.ReadToEndAsync();
                try
                {
                    recivedData = JsonSerializer.Deserialize<TE>(body);
                    if (recivedData != null)
                    {
                        if (CheckAllowed<T, TE>(dbcontext, recivedData, mi, out Reason))
                        {
                            dbcontext.Set<TE>().Remove(recivedData);
                            DoBeforeContextSaveChanges<T>(dbContextBeforeSaveChangesMethod, dbcontext);
                            await dbcontext.SaveChangesAsync();
                            context.Response.Headers.ContentType="text/plain";
                            context.Response.Headers.ContentEncoding = "utf-8";                             
                            await context.Response.WriteAsync("Deleted");
                        }
                        else
                        {
                            await context.Response.WriteAsync(Reason);
                        }
                    }
                }
                catch
                {
                    // It can be array
                    try
                    {
                        recivedDataList = JsonSerializer.Deserialize<List<TE>>(body);
                        if (recivedDataList != null)
                        {
                            if (CheckAllowedList<T, TE>(dbcontext, recivedDataList, mi, out Reason))
                            {
                                dbcontext.Set<TE>().RemoveRange(recivedDataList);
                                DoBeforeContextSaveChanges<T>(dbContextBeforeSaveChangesMethod, dbcontext);
                                await dbcontext.SaveChangesAsync();
                                context.Response.Headers.ContentType="text/plain";
                                context.Response.Headers.ContentEncoding = "utf-8";                                  
                                await context.Response.WriteAsync("Deleted");
                            }
                            else
                            {
                                context.Response.Headers.ContentType="text/plain";
                                context.Response.Headers.ContentEncoding = "utf-8";                                  
                                await context.Response.WriteAsync(Reason);
                            }
                        }
                    }
                    catch
                    {

                    }
                }
            }
            else if (options.InteractingType == InteractingType.XML)
            {
                using var reader = new StreamReader(context.Request.Body);
                var body = await reader.ReadToEndAsync();

                Stream stream = GenerateStreamFromString(body);
                XmlRootAttribute a = new("result");
                XmlSerializer serializer = new(typeof(List<TE>), a);
                var recivedDataL = (List<TE>?)serializer.Deserialize(stream);
                await stream.DisposeAsync();
                if (recivedDataL != null && recivedDataL.Count > 0)
                {
                    if (CheckAllowedList<T, TE>(dbcontext, recivedDataL, mi, out Reason))
                    {
                        dbcontext.Set<TE>().RemoveRange(recivedDataL);
                        DoBeforeContextSaveChanges<T>(dbContextBeforeSaveChangesMethod, dbcontext);
                        await dbcontext.SaveChangesAsync();
                        context.Response.Headers.ContentType="text/plain";
                        context.Response.Headers.ContentEncoding = "utf-8";                          
                        await context.Response.WriteAsync("Deleted");
                    }
                    else
                    {
                        context.Response.Headers.ContentType="text/plain";
                        context.Response.Headers.ContentEncoding = "utf-8";                          
                        await context.Response.WriteAsync(Reason);
                    }
                }
                // Do something
            }
        };
    }
    private static MethodInfo GetGenericMethod(string name, Type[] types)
    {
        MethodInfo mi = typeof(Handler).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(v => v.Name == name).FirstOrDefault() ?? throw (new ArgumentException("Method " + name + " does not exists", "name"));
        MethodInfo miGeneric = mi.MakeGenericMethod(types);
        return miGeneric;
    }
    /// <summary>
    /// Get the Handler for client request
    ///
    /// </summary>
    /// <param name="name">The method name that will be invoked</param>
    /// <param name="types">The generic type parameters</param>
    /// <param name="instance">The object instance for execution</param>
    /// <param name="args">The arguments for invoked method</param>
    public static RequestDelegate GetRequestDelegate(string name, Type[] types, object instance, object?[] args)
    {
        return (RequestDelegate)GetGenericMethod(name, types).Invoke(instance, args)!;
    }
}
