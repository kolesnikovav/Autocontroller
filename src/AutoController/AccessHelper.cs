using System;
using System.Linq;
using System.Reflection;
using System.Net.Http;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AutoController;
/// <summary>
/// Helper class for access rights keys
/// </summary>
public static class AccessHelper
{
    /// <summary>
    /// Retrive string access permition key
    /// </summary>
    public static string GetAccessKey(Type givenType, PropertyInfo? property, HttpMethod method)
    {
        string? p = (property == null) ? "null" : property.ToString();
        return givenType.ToString() + p + method.ToString();
    }
    /// <summary>
    /// Evaluate access right
    /// </summary>
    /// <param name="restrictions">Restrictions for evaluate</param>
    /// <param name="context">Http context</param>
    public static bool EvaluateAccessRightsAsync(HttpContext context, List<AuthorizeAttribute> restrictions)
    {
        bool result = false;
        foreach (var r in restrictions)
        {
            if (!string.IsNullOrWhiteSpace(r.Roles))
            {
                var roles = r.Roles.IndexOf(",") == -1 ? new string[] { r.Roles } : r.Roles.Split(",").Where(v => !string.IsNullOrWhiteSpace(v));
                foreach (var rL in roles)
                {
                    if (context.User.IsInRole(rL)) return true; // success
                }
            }
            if (!string.IsNullOrWhiteSpace(r.Policy))
            {
                var policies = r.Policy.IndexOf(",") == -1 ? new string[] { r.Policy } : r.Policy.Split(",").Where(v => !string.IsNullOrWhiteSpace(v));
                var serv = context.RequestServices.GetServices<IAuthorizationService>();
                foreach (var w in serv)
                {
                    foreach (var p in policies)
                    {
                        var res = w.AuthorizeAsync(context.User, p).Result;
                        if (res.Succeeded) return true;
                    }
                }
            }
            // TODO Policy rights evaluated
        }
        return result;
    }
}
