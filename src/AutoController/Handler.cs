using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Encodings;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace AutoController
{
    internal static class Handler
    {
        private static DbContextOptionsBuilder<T> GetDBSpecificOptionsBuilder<T>(DatabaseTypes dbType, string connString) where T : DbContext, IDisposable
        {
            if (dbType == DatabaseTypes.SQLite) return SQLiteProvider<T>.GetBuilder(connString);
            if (dbType == DatabaseTypes.SQLServer) return SQLServerProvider<T>.GetBuilder(connString);
            return PostgresProvider<T>.GetBuilder(connString);
        }
        private static RequestDelegate GetHandlerPage<T, TE>(
            DatabaseTypes dbType,
            string connString,
            uint pageSize,
            uint pageNumber,
            InteractingType interactingType) where T : DbContext, IDisposable
                                             where TE : class
        {
            return async (context) =>
            {
                int skip = (int)((pageNumber - 1) * pageSize);

                IEnumerable<TE> queryResult;
                var optionsBuilder = GetDBSpecificOptionsBuilder<T>(dbType, connString);
                Type t = typeof(T);
                using (T dbcontext = (T)Activator.CreateInstance(t, new object[] { optionsBuilder.Options }))
                {
                    if (pageSize == 0)
                    {
                        queryResult = dbcontext.Set<TE>().AsNoTracking().ToList<TE>();
                    }
                    else
                    {
                        queryResult = dbcontext.Set<TE>().AsNoTracking().Skip(skip).Take((int)pageSize).ToList<TE>();
                    }
                }
                if (interactingType == InteractingType.JSON)
                {
                    byte[] jsonUtf8Bytes;
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(queryResult, options);
                    await context.Response.WriteAsync(System.Text.Encoding.UTF8.GetString(jsonUtf8Bytes));
                    context.Response.Headers.Add("ContentType","text/json");
                    context.Response.StatusCode = StatusCodes.Status200OK;

                }
                else if (interactingType == InteractingType.XML)
                {
                    XmlRootAttribute a = new XmlRootAttribute("result");
                    XmlSerializer serializer = new XmlSerializer(typeof(TE),a);
                    StringWriter textWriter = new StringWriter();
                    foreach(TE o in queryResult)
                    {
                        serializer.Serialize(textWriter,o);
                    }
                    await context.Response.WriteAsync(textWriter.ToString());
                    context.Response.Headers.Add("ContentType","text/xml");
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await textWriter.DisposeAsync();
                }
            };
        }
        private static RequestDelegate GetHandler<T, TE>(
            DatabaseTypes dbType,
            string connString,
            InteractingType interactingType) where T : DbContext, IDisposable
                                             where TE : class
        {
            return GetHandlerPage<T, TE>(dbType, connString, (uint)0, (uint)0, interactingType);
        }
        private static RequestDelegate GetCountOf<T, TE>(DatabaseTypes dbType, string connString) where T : DbContext, IDisposable
                                                                                                 where TE : class
        {
            return async (context) =>
            {
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
        public static RequestDelegate GetRequestDelegate(string name, Type[] types, object instance, InteractingType interactingType, object[] args)
        {
            return (RequestDelegate)GetGenericMethod(name, types).Invoke(instance, args);
        }
    }
}