// using System;
// using Microsoft.AspNetCore.Http;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.Routing;
// using Microsoft.AspNetCore.Mvc;
// using System.Reflection;
// using Microsoft.AspNetCore.Builder;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Logging.Abstractions;

// namespace AutoController
// {
//     // public static class Handler
//     // {
//     //     public static async Task HandleGetDefault(RouteContext context)
//     //     {
//     //         var r  = context.Handler.Invoke?().RouteData.DataTokens;
//     //         await context.Response.WriteAsync("");
//     //     }
//     // }
// }

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;

namespace AutoController
{
    public class AdminRoute : IRouter
    {
        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            throw new NotImplementedException();
        }

        public async Task RouteAsync(RouteContext context)
        {
            string url = context.HttpContext.Request.Path.Value.TrimEnd('/');
            if (url.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
            {
                context.Handler = async ctx =>
                {
                    ctx.Response.ContentType = "text/html;charset=utf-8";
                    await ctx.Response.WriteAsync("Привет admin!");
                };
            }
            await Task.CompletedTask;
        }
    }
}