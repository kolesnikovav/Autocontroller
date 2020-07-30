using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;

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
        private static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
        private static RequestDelegate GetHandler<T, TE>(
            DatabaseTypes dbType,
            string connString,
            InteractingType interactingType,
            JsonSerializerOptions jsonSerializerOptions = null) where T : DbContext, IDisposable
                                                                where TE : class
        {
            return async (context) =>
            {
                uint pageSize = 0;
                uint pageNumber = 0;
                var RequestParams = context.Request.Query;
                if (RequestParams.ContainsKey("page"))
                {
                    UInt32.TryParse(RequestParams["page"], out pageNumber);
                }
                if (RequestParams.ContainsKey("pagesize"))
                {
                    UInt32.TryParse(RequestParams["pagesize"], out pageSize);
                }
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
        private static RequestDelegate PostHandler<T, TE>(
            DatabaseTypes dbType,
            string connString,
            InteractingType interactingType,
            JsonSerializerOptions jsonSerializerOptions = null) where T : DbContext, IDisposable
                                                                where TE : class
        {
            return async (context) =>
            {
                TE recivedData;

                var optionsBuilder = GetDBSpecificOptionsBuilder<T>(dbType, connString);
                Type t = typeof(T);
                using (T dbcontext = (T)Activator.CreateInstance(t, new object[] { optionsBuilder.Options }))
                {
                    if (interactingType == InteractingType.JSON)
                    {
                        using (var reader = new StreamReader(context.Request.Body))
                        {
                            var body = await reader.ReadToEndAsync();
                            recivedData = JsonSerializer.Deserialize<TE>(body);
                            if (recivedData != null)
                            {
                                dbcontext.Set<TE>().Add(recivedData);
                                await dbcontext.SaveChangesAsync();
                                byte[] jsonUtf8Bytes;
                                jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(recivedData, jsonSerializerOptions);
                                await context.Response.WriteAsync(System.Text.Encoding.UTF8.GetString(jsonUtf8Bytes));
                            }
                            // Do something
                        }
                    }
                    else if (interactingType == InteractingType.XML)
                    {
                        using (var reader = new StreamReader(context.Request.Body))
                        {
                            var body = await reader.ReadToEndAsync();

                            Stream stream = GenerateStreamFromString(body);
                            XmlRootAttribute a = new XmlRootAttribute("result");
                            XmlSerializer serializer = new XmlSerializer(typeof(TE),a);
                            recivedData = (TE)serializer.Deserialize(stream);
                            await stream.DisposeAsync();
                            if (recivedData != null)
                            {
                                dbcontext.Set<TE>().Add(recivedData);
                                await dbcontext.SaveChangesAsync();
                                byte[] jsonUtf8Bytes;
                                jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(recivedData, jsonSerializerOptions);
                                await context.Response.WriteAsync(System.Text.Encoding.UTF8.GetString(jsonUtf8Bytes));
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
        public static RequestDelegate GetRequestDelegate(string name, Type[] types, object instance, object[] args)
        {
            return (RequestDelegate)GetGenericMethod(name, types).Invoke(instance, args);
        }
    }
}