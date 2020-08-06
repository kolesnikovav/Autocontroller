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

namespace AutoController
{
    internal static class Handler
    {
        private static MethodInfo GetActionBeforeSave<TE>() where TE: class
        {
            return typeof(TE).GetMethod("DoBeforeSave");
        }
        private static MethodInfo GetActionBeforeDelete<TE>() where TE: class
        {
            return typeof(TE).GetMethod("DoBeforeDelete");
        }
        private static DbContextOptionsBuilder<T> GetDBSpecificOptionsBuilder<T>(DatabaseTypes dbType, string connString) where T : DbContext, IDisposable
        {
            if (dbType == DatabaseTypes.SQLite) return SQLiteProvider<T>.GetBuilder(connString);
            if (dbType == DatabaseTypes.SQLServer) return SQLServerProvider<T>.GetBuilder(connString);
            return PostgresProvider<T>.GetBuilder(connString);
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
                                                Dictionary<string,List<AuthorizeAttribute>> restrictions,
                                                bool allowAnonimus,
                                                string authentificationPath,
                                                string accessDeniedPath)
        {
            //var rr = AccessHelper.EvaluateAccessRightsAsync<TE>(context.User, requestMethod, restrictions)
            if (allowAnonimus && context.Request.Method == HttpMethods.Get) return true;
            string AKey = AccessHelper.GetAccessKey(typeof(TE), null,requestMethod);
            if (!restrictions.ContainsKey(AKey)) return true; // no restrictions!
            if (!context.User.Identity.IsAuthenticated)
            {
                context.Response.Redirect(authentificationPath);
                if (!context.User.Identity.IsAuthenticated)
                {
                    context.Response.Redirect(authentificationPath);
                    return false;
                }
            }
            else // user is authentificated
            {
               return AccessHelper.EvaluateAccessRightsAsync(context.User, restrictions[AKey]);
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
                if (!String.IsNullOrWhiteSpace(QueryParams.sort) && typeof(TE).GetProperty(QueryParams.sort) != null)
                {
                    if (!String.IsNullOrWhiteSpace(QueryParams.sortDirection))
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
                if (!String.IsNullOrWhiteSpace(QueryParams.sort) && typeof(TE).GetProperty(QueryParams.sort) != null)
                {
                    if (!String.IsNullOrWhiteSpace(QueryParams.sortDirection))
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
            Dictionary<string,List<AuthorizeAttribute>> restrictions,
            Dictionary<Type, EntityKeyDescribtion> entityKeys,
            DatabaseTypes dbType,
            string connString,
            InteractingType interactingType,
            bool allowAnonimus,
            string authentificationPath,
            string accessDeniedPath,
            Dictionary<string,RequestParamName> _requestParams,
            JsonSerializerOptions jsonSerializerOptions = null) where T : DbContext, IDisposable
                                                                where TE : class
        {
            return async (context) =>
            {
                bool authResult = Authorization<TE>(context, HttpMethod.Get, restrictions, allowAnonimus,authentificationPath,accessDeniedPath);
                if (!authResult)
                {
                    return;
                }
                var e = entityKeys;
                var QueryParams = RequestParams.RetriveQueryParam(context.Request.Query, _requestParams);
                IEnumerable<TE> queryResult;
                var optionsBuilder = GetDBSpecificOptionsBuilder<T>(dbType, connString);
                Type t = typeof(T);
                using (T dbcontext = (T)Activator.CreateInstance(t, new object[] { optionsBuilder.Options }))
                {
                    queryResult = GetDBQueryResult<T, TE>(dbcontext, QueryParams);
                }
                if (interactingType == InteractingType.JSON)
                {
                    byte[] jsonUtf8Bytes;
                    jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(queryResult, jsonSerializerOptions);
                    await context.Response.WriteAsync(System.Text.Encoding.UTF8.GetString(jsonUtf8Bytes));
                }
                else if (interactingType == InteractingType.XML)
                {
                    XmlRootAttribute a = new XmlRootAttribute("result");
                    // XmlSerializer does not support IEnumerable<T>
                    XmlSerializer serializer = new XmlSerializer(typeof(List<TE>),a);
                    StringWriter textWriter = new StringWriter();
                    serializer.Serialize(textWriter,queryResult.ToList());
                    await context.Response.WriteAsync(textWriter.ToString());
                    await textWriter.DisposeAsync();
                }
            };
        }
        private static RequestDelegate GetCountOf<T, TE>(Dictionary<string,List<AuthorizeAttribute>> restrictions,
                                                         Dictionary<Type, EntityKeyDescribtion> entityKeys,
                                                         DatabaseTypes dbType,
                                                         string connString,
                                                         bool allowAnonimus,
                                                         string authentificationPath,
                                                         string accessDeniedPath,
                                                         Dictionary<string,RequestParamName> requestParams) where T : DbContext, IDisposable
                                                                                                            where TE : class
        {
            return async (context) =>
            {
                bool authResult = Authorization<TE>(context, HttpMethod.Get, restrictions, allowAnonimus,authentificationPath,accessDeniedPath);
                if (!authResult)
                {
                    return;
                }
                var QueryParams = RequestParams.RetriveQueryParam(context.Request.Query, requestParams);
                int queryResult;
                var optionsBuilder = GetDBSpecificOptionsBuilder<T>(dbType, connString);
                Type t = typeof(T);
                using (T dbcontext = (T)Activator.CreateInstance(t, new object[] { optionsBuilder.Options }))
                {
                    queryResult = dbcontext.Set<TE>().AsNoTracking().Count();
                }
                await context.Response.WriteAsync(queryResult.ToString());
            };
        }
        private static bool CheckAllowed<T, TE>(T dbcontext, TE recivedObject, MethodInfo methodInfo, out string reason) where T : DbContext, IDisposable
                                                                                                          where TE : class
        {
            reason = "";
            bool result = true;
            if (methodInfo == null) return result;
            object[] p = new object[] {dbcontext, reason};
            result = (bool)methodInfo.Invoke(recivedObject, p);
            reason += (string)p[1];
            return result;
        }
        private static bool CheckAllowedList<T, TE>(T dbcontext, List<TE> recivedObjects, MethodInfo methodInfo, out string reason) where T : DbContext, IDisposable
                                                                                                          where TE : class
        {
            reason = "";
            bool result = true;
            if (methodInfo == null) return result;
            object[] p = new object[] {dbcontext, reason};
            foreach( TE el in recivedObjects)
            {
                result = (bool)methodInfo.Invoke(el, p) && result;
                reason += (string)p[1] + "\n";
            }
            return result;
        }

        private static RequestDelegate PostHandler<T, TE>(
            bool update,
            Dictionary<string,List<AuthorizeAttribute>> restrictions,
            DatabaseTypes dbType,
            string connString,
            InteractingType interactingType,
            string authentificationPath,
            string accessDeniedPath,
            JsonSerializerOptions jsonSerializerOptions = null) where T : DbContext, IDisposable
                                                                where TE : class
        {
            return async (context) =>
            {
                bool authResult = Authorization<TE>(context, HttpMethod.Post, restrictions, false,authentificationPath,accessDeniedPath);
                if (!authResult)
                {
                    return;
                }
                TE recivedData;
                List<TE> recivedDataList;
                var mi = GetActionBeforeSave<TE>();
                string Reason = "";

                var optionsBuilder = GetDBSpecificOptionsBuilder<T>(dbType, connString);
                Type t = typeof(T);
                using (T dbcontext = (T)Activator.CreateInstance(t, new object[] { optionsBuilder.Options }))
                {
                    if (interactingType == InteractingType.JSON)
                    {
                        using (var reader = new StreamReader(context.Request.Body))
                        {
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
                                        await dbcontext.SaveChangesAsync();
                                        byte[] jsonUtf8Bytes;
                                        jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(recivedData, jsonSerializerOptions);
                                        await context.Response.WriteAsync(System.Text.Encoding.UTF8.GetString(jsonUtf8Bytes));
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
                                            await dbcontext.SaveChangesAsync();
                                            byte[] jsonUtf8Bytes;
                                            jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(recivedDataList, jsonSerializerOptions);
                                            await context.Response.WriteAsync(System.Text.Encoding.UTF8.GetString(jsonUtf8Bytes));
                                        }
                                    }
                                }
                                catch
                                {

                                }
                            }
                        }
                    }
                    else if (interactingType == InteractingType.XML)
                    {
                        using (var reader = new StreamReader(context.Request.Body))
                        {
                            var body = await reader.ReadToEndAsync();

                            Stream stream = GenerateStreamFromString(body);
                            XmlRootAttribute a = new XmlRootAttribute("result");
                            XmlSerializer serializer = new XmlSerializer(typeof(List<TE>),a);
                            var recivedDataL = (List<TE>)serializer.Deserialize(stream);
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
                                    await dbcontext.SaveChangesAsync();
                                    StringWriter textWriter = new StringWriter();
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
                    }
                }
            };
        }
        private static RequestDelegate DeleteHandler<T, TE>(
            Dictionary<string,List<AuthorizeAttribute>> restrictions,
            DatabaseTypes dbType,
            string connString,
            InteractingType interactingType,
            string authentificationPath,
            string accessDeniedPath,
            JsonSerializerOptions jsonSerializerOptions = null) where T : DbContext, IDisposable
                                                                where TE : class
        {
            return async (context) =>
            {
                bool authResult = Authorization<TE>(context, HttpMethod.Delete, restrictions, false,authentificationPath,accessDeniedPath);
                if (!authResult)
                {
                    return;
                }
                TE recivedData;
                List<TE> recivedDataList;
                var mi = GetActionBeforeDelete<TE>();
                string Reason ="";

                var optionsBuilder = GetDBSpecificOptionsBuilder<T>(dbType, connString);
                Type t = typeof(T);
                using (T dbcontext = (T)Activator.CreateInstance(t, new object[] { optionsBuilder.Options }))
                {
                    if (interactingType == InteractingType.JSON)
                    {
                        using (var reader = new StreamReader(context.Request.Body))
                        {
                            var body = await reader.ReadToEndAsync();
                            try
                            {
                                recivedData = JsonSerializer.Deserialize<TE>(body);
                                if (recivedData != null)
                                {
                                    if (CheckAllowed<T, TE>(dbcontext, recivedData, mi, out Reason))
                                    {
                                        dbcontext.Set<TE>().Remove(recivedData);
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
                    }
                    else if (interactingType == InteractingType.XML)
                    {
                        using (var reader = new StreamReader(context.Request.Body))
                        {
                            var body = await reader.ReadToEndAsync();

                            Stream stream = GenerateStreamFromString(body);
                            XmlRootAttribute a = new XmlRootAttribute("result");
                            XmlSerializer serializer = new XmlSerializer(typeof(List<TE>),a);
                            var recivedDataL = (List<TE>)serializer.Deserialize(stream);
                            await stream.DisposeAsync();
                            if (recivedDataL != null && recivedDataL.Count > 0)
                            {
                                if (CheckAllowedList<T, TE>(dbcontext, recivedDataL, mi, out Reason))
                                {
                                    dbcontext.Set<TE>().RemoveRange(recivedDataL);
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
                    }
                }
            };
        }
        private static MethodInfo GetGenericMethod(string name, Type[] types)
        {
            MethodInfo mi = typeof(Handler).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where( v => v.Name == name).FirstOrDefault();
            if (mi == null)
            {
                throw(new ArgumentException("Method "+ name +" does not exists","name"));
            }
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
        public static RequestDelegate GetRequestDelegate(string name, Type[] types, object instance, object[] args)
        {
            return (RequestDelegate)GetGenericMethod(name, types).Invoke(instance, args);
        }
    }
}