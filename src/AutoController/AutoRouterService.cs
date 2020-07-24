using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AutoController
{
    public class AutoRouterService
    {
        public Dictionary<string, Type> _autoroutes = new Dictionary<string, Type>();
        private string _routePrefix;
        private Type MapToControllerAttributeType = typeof(MapToControllerAttribute);
        private Type MapToControllerGetParamAttributeType = typeof(MapToControllerGetParamAttribute);
        private void AddRoutesForProperties(Type givenType, string routeClassName)
        {
            foreach (PropertyInfo pInfo in givenType.GetProperties())
            {
                MapToControllerGetParamAttribute b = pInfo.GetCustomAttribute(MapToControllerGetParamAttributeType) as MapToControllerGetParamAttribute;
                if (b != null)
                {
                    string r = String.IsNullOrWhiteSpace(b.ParamName) ? pInfo.Name : b.ParamName;
                    string optional = b.Optional ? "?" : String.Empty;
                    string route = routeClassName + "/{" + r + optional + "}";
                    //"{controller}/{property?}"
                    _autoroutes.Add(route, pInfo.PropertyType);
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
                    string routeName = String.IsNullOrWhiteSpace(r.ControllerName) ? givenType.Name : r.ControllerName;
                    if (!_autoroutes.ContainsKey(routeName))
                    {
                        _autoroutes.Add(routeName,givenType);
                        AddRoutesForProperties(givenType, routeName);
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
        public void GetAutoControllers(Type typeForAutocontroller, string routePrefix)
        {
            _routePrefix = routePrefix;
            ProcessType (typeForAutocontroller);
            PropertyInfo[] p = typeForAutocontroller.GetProperties();
            foreach (PropertyInfo t in p)
            {
                ProcessType (t.PropertyType);
            }
        }
    }
}