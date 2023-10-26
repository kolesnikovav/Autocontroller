using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;

using System.Xml.Serialization;
using System.IO;
using System.Net.Http;
using System.Linq.Dynamic;

namespace AutoController;
internal static class Handler
{
    private static MethodInfo? GetActionBeforeSave<TE>() where TE : class
    {
        return typeof(TE).GetRuntimeMethod("DoBeforeSave", new Type[] { typeof(DbContext), typeof(string) });
    }
    private static MethodInfo? GetActionBeforeDelete<TE>() where TE : class
    {
        return typeof(TE).GetRuntimeMethod("DoBeforeDelete", new Type[] { typeof(DbContext), typeof(string) });
    }
    private static DbContextOptionsBuilder<T> GetDBSpecificOptionsBuilder<T>(DatabaseTypes dbType, string connString, DbContextOptions<T>? dbContextOptions = null) where T : DbContext, IDisposable
    {
        if (dbContextOptions == null)
        {
            return dbType switch
            {
                DatabaseTypes.SQLite => SQLiteProvider<T>.GetBuilder(connString),
                DatabaseTypes.SQLServer => SQLServerProvider<T>.GetBuilder(connString),
                DatabaseTypes.Postgres => PostgresProvider<T>.GetBuilder(connString),
                DatabaseTypes.MySQL => MySQLProvider<T>.GetBuilder(connString),
                _ => InMemoryProvider<T>.GetBuilder(connString),
            };
        }
        else
        {
            return dbType switch
            {
                DatabaseTypes.SQLite => SQLiteProvider<T>.GetBuilder(connString, dbContextOptions),
                DatabaseTypes.SQLServer => SQLServerProvider<T>.GetBuilder(connString, dbContextOptions),
                DatabaseTypes.Postgres => PostgresProvider<T>.GetBuilder(connString, dbContextOptions),
                DatabaseTypes.MySQL => MySQLProvider<T>.GetBuilder(connString, dbContextOptions),
                _ => InMemoryProvider<T>.GetBuilder(connString, dbContextOptions),
            };

        }
    }
    private static T CreateContext<T>(string connString, DatabaseTypes dbType, Func<T>? factory = null, DbContextOptions<T>? dbContextOptions = null) where T : DbContext, IDisposable
    {
        if (factory != null) return factory();
        var optionsBuilder = GetDBSpecificOptionsBuilder<T>(dbType, connString, dbContextOptions);

        var options = optionsBuilder.Options;
        return (T)Activator.CreateInstance(typeof(T), new object[] { options })!;
    }

    private static Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
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
        if (!restrictions.ContainsKey(AKey)) return true; // no restrictions!
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
            return AccessHelper.EvaluateAccessRightsAsync(context, restrictions[AKey]);
        }
        return true;
    }
    private static IEnumerable<TE> GetDBQueryResult<T, TE>(T dbcontext, UserRequestParams QueryParams) where T : DbContext, IDisposable
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
                    queryResult = dbcontext.Set<TE>().AsNoTracking().Where(QueryParams.filter).OrderByDescending(QueryParams.sort).ToList<TE>();
                }
                else
                {
                    queryResult = dbcontext.Set<TE>().AsNoTracking().Where(QueryParams.filter).OrderBy(QueryParams.sort).ToList<TE>();
                }
            }
            else
            {
                queryResult = dbcontext.Set<TE>().AsNoTracking().Where(QueryParams.filter).ToList<TE>();
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(QueryParams.sort) && typeof(TE).GetProperty(QueryParams.sort) != null)
            {
                if (!string.IsNullOrWhiteSpace(QueryParams.sortDirection))
                {
                    queryResult = dbcontext.Set<TE>().AsNoTracking().Where(QueryParams.filter).OrderByDescending(QueryParams.sort).Skip(skip).Take((int)QueryParams.pageSize).ToList<TE>();
                }
                else
                {
                    queryResult = dbcontext.Set<TE>().AsNoTracking().Where(QueryParams.filter).OrderBy(QueryParams.sort).Skip(skip).Take((int)QueryParams.pageSize).ToList<TE>();
                }
            }
            else
            {
                queryResult = dbcontext.Set<TE>().AsNoTracking().Where(QueryParams.filter).Skip(skip).Take((int)QueryParams.pageSize).ToList<TE>();
            }
        }
        return queryResult;
    }
    private static RequestDelegate GetHandler<T, TE>(
        Dictionary<string, List<AuthorizeAttribute>> restrictions,
        Dictionary<Type, EntityKeyDescribtion> entityKeys,
        DatabaseTypes dbType,
        string connString,
        InteractingType interactingType,
        bool allowAnonimus,
        string authentificationPath,
        string accessDeniedPath,
        Dictionary<string, RequestParamName> _requestParams,
        JsonSerializerOptions? jsonSerializerOptions = null,
        Func<T>? customDbContextFactory = null,
        DbContextOptions<T>? customDbContextOptions = null) where T : DbContext, IDisposable
                                                            where TE : class
    {
        return async (context) =>
        {
            bool authResult = Authorization<TE>(context, HttpMethod.Get, restrictions, allowAnonimus, authentificationPath, accessDeniedPath);
            if (!authResult)
            {
                return;
            }
            var e = entityKeys;
            var QueryParams = RequestParams.RetriveQueryParam(context.Request.Query, _requestParams);
            IEnumerable<TE> queryResult;

            using (T dbcontext = CreateContext<T>(connString, dbType, customDbContextFactory, customDbContextOptions))
            {
                queryResult = GetDBQueryResult<T, TE>(dbcontext, QueryParams);
            }
            if (interactingType == InteractingType.JSON)
            {
                byte[] jsonUtf8Bytes;
                jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(queryResult, jsonSerializerOptions);
                await context.Response.WriteAsync(Encoding.UTF8.GetString(jsonUtf8Bytes));
            }
            else if (interactingType == InteractingType.XML)
            {
                XmlRootAttribute a = new("result");
                // XmlSerializer does not support IEnumerable<T>
                XmlSerializer serializer = new(typeof(List<TE>), a);
                StringWriter textWriter = new();
                serializer.Serialize(textWriter, queryResult.ToList());
                await context.Response.WriteAsync(textWriter.ToString());
                await textWriter.DisposeAsync();
            }
        };
    }
    private static RequestDelegate GetCountOf<T, TE>(Dictionary<string, List<AuthorizeAttribute>> restrictions,
                                                     Dictionary<Type, EntityKeyDescribtion> entityKeys,
                                                     DatabaseTypes dbType,
                                                     string connString,
                                                     bool allowAnonimus,
                                                     string authentificationPath,
                                                     string accessDeniedPath,
                                                     Dictionary<string, RequestParamName> requestParams,
                                                     Func<T>? customDbContextFactory = null,
                                                     DbContextOptions<T>? customDbContextOptions = null) where T : DbContext, IDisposable
                                                                                                        where TE : class
    {
        return async (context) =>
        {
            bool authResult = Authorization<TE>(context, HttpMethod.Get, restrictions, allowAnonimus, authentificationPath, accessDeniedPath);
            if (!authResult)
            {
                return;
            }
            var QueryParams = RequestParams.RetriveQueryParam(context.Request.Query, requestParams);
            int queryResult;

            using (T dbcontext = CreateContext<T>(connString, dbType, customDbContextFactory,customDbContextOptions))
            {
                queryResult = GetDBQueryResult<T, TE>(dbcontext, QueryParams).Count<TE>();
            }
            await context.Response.WriteAsync(queryResult.ToString());
        };
    }
    private static bool CheckAllowed<T, TE>(T dbcontext, TE recivedObject, MethodInfo? methodInfo, out string reason) where T : DbContext, IDisposable
                                                                                                      where TE : class
    {
        reason = "";
        bool result = true;
        if (methodInfo == null) return result;
        object[] p = new object[] { dbcontext, reason };
        result = (bool?)methodInfo.Invoke(recivedObject, p) ?? true;
        reason += (string)p[1];
        return result;
    }
    private static bool CheckAllowedList<T, TE>(T dbcontext, List<TE> recivedObjects, MethodInfo? methodInfo, out string reason) where T : DbContext, IDisposable
                                                                                                      where TE : class
    {
        reason = "";
        bool result = true;
        if (methodInfo == null) return result;
        object[] p = new object[] { dbcontext, reason };
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
        DatabaseTypes dbType,
        string connString,
        InteractingType interactingType,
        string authentificationPath,
        string accessDeniedPath,
        JsonSerializerOptions? jsonSerializerOptions = null,
        MethodInfo? dbContextBeforeSaveChangesMethod = null,
        Func<T>? customDbContextFactory = null,
        DbContextOptions<T>? customDbContextOptions = null) where T : DbContext, IDisposable
                                                            where TE : class
    {
        return async (context) =>
        {
            bool authResult = Authorization<TE>(context, HttpMethod.Post, restrictions, false, authentificationPath, accessDeniedPath);
            if (!authResult)
            {
                return;
            }
            TE? recivedData;
            List<TE>? recivedDataList;
            var mi = GetActionBeforeSave<TE>();
            string Reason = "";

            using T dbcontext = CreateContext<T>(connString, dbType, customDbContextFactory, customDbContextOptions);
            if (interactingType == InteractingType.JSON)
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
                            jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(recivedData, jsonSerializerOptions);
                            await context.Response.WriteAsync(Encoding.UTF8.GetString(jsonUtf8Bytes));
                        }
                        else
                        {
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
                                jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(recivedDataList, jsonSerializerOptions);
                                await context.Response.WriteAsync(Encoding.UTF8.GetString(jsonUtf8Bytes));
                            }
                        }
                    }
                    catch
                    {

                    }
                }
            }
            else if (interactingType == InteractingType.XML)
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
        DatabaseTypes dbType,
        string connString,
        InteractingType interactingType,
        string authentificationPath,
        string accessDeniedPath,
        JsonSerializerOptions? jsonSerializerOptions = null,
        MethodInfo? dbContextBeforeSaveChangesMethod = null,
        Func<T>? customDbContextFactory = null,
        DbContextOptions<T>? customDbContextOptions = null) where T : DbContext, IDisposable
                                                            where TE : class
    {
        return async (context) =>
        {
            bool authResult = Authorization<TE>(context, HttpMethod.Delete, restrictions, false, authentificationPath, accessDeniedPath);
            if (!authResult)
            {
                return;
            }
            TE? recivedData;
            List<TE>? recivedDataList;
            var mi = GetActionBeforeDelete<TE>();
            string Reason = "";

            using T dbcontext = CreateContext<T>(connString, dbType, customDbContextFactory, customDbContextOptions);
            if (interactingType == InteractingType.JSON)
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

                    }
                }
            }
            else if (interactingType == InteractingType.XML)
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
                        await context.Response.WriteAsync("Deleted");
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
