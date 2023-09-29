using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace AutoController;
/// <summary>
/// Contains information about entity keys
/// </summary>
public struct EntityKeyDescribtion
{
    /// <summary>
    /// Name of entity key
    /// </summary>
    public string Name;
    /// <summary>
    /// Type of entity key
    /// </summary>
    public Type KeyType;
    /// <summary>
    /// Retrives parameter expression from this describtion for use in Linq queries
    /// </summary>
    public readonly ParameterExpression GetParameter()
    {
        return Expression.Parameter(KeyType, "x");
    }
}
