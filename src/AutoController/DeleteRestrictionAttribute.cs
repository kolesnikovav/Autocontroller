using System;
using Microsoft.AspNetCore.Authorization;

namespace AutoController
{
    /// <summary>
    /// Specifies that DELETE request to class or property requires the specified authorization.
    /// Inherited from AuthorizeAttribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class , AllowMultiple = true, Inherited = true)]
    public class DeleteRestictionAttribute: AuthorizeAttribute
    {

    }
}