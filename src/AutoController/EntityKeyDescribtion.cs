using System;
using System.Linq.Expressions;

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
