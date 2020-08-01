using System;
using System.Linq;
using System.Reflection;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

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
        /// <summary>
        /// Evaluate access right
        /// </summary>
        /// <param name="user">Current user from context</param>
        /// <param name="restrictions">Restrictions for evaluate</param>
        public static bool EvaluateAccessRightsAsync(System.Security.Claims.ClaimsPrincipal user, List<AuthorizeAttribute> restrictions)
        {
            bool result = false;
            foreach( var r in restrictions)
            {
                if (!String.IsNullOrWhiteSpace(r.Roles))
                {
                    var roles = r.Roles.IndexOf(",") == -1 ? new string[] {r.Roles} : r.Roles.Split(",").Where( v => !String.IsNullOrWhiteSpace(v));
                    foreach (var rL in roles)
                    {
                        if (user.IsInRole(rL)) return true; // success
                    }
                }
                // TODO Policy rights evaluated
            }
            return result;
        }
    }
}