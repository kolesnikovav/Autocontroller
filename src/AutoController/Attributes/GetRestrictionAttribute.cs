using System;
using Microsoft.AspNetCore.Authorization;

namespace AutoController;

/// <summary>
/// Specifies that GET request to class or property requires the specified authorization.
/// Inherited from AuthorizeAttribute
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
public class GetRestrictionAttribute : AuthorizeAttribute
{

}

