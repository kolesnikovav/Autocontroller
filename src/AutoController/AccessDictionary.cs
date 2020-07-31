using System;
using System.Reflection;
using System.Net.Http;

namespace AutoController
{
    /// <summary>
    /// Helper class for access rights keys
    /// </summary>
    public static class AccessHelper
    {
    /// <summary>
    /// Retrive string access permition key
    /// </summary>
        public static string GetAccessKey(Type givenType, PropertyInfo property, HttpMethod method)
        {
            string p = (property == null) ? "null" : property.ToString();
            return givenType.ToString() + p + method.ToString();
        }
    }
}