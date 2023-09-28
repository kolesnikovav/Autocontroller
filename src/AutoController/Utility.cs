// Getting from https://www.codeproject.com/Tips/817372/Building-OrderBy-Lambda-Expression-from-Property-N
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Dynamic;
using System.Linq.Dynamic;

namespace AutoController;
/// <summary>
/// Utility
/// </summary>
public static class Utility
{
    /// <summary>
    ///makes expression for specific prop
    /// </summary>
    public static Expression<Func<TSource, object>> GetExpression<TSource>(string propertyName)
    {
        var param = Expression.Parameter(typeof(TSource), "x");
        Expression conversion = Expression.Convert(Expression.Property
        (param, propertyName), typeof(object));   //important to use the Expression.Convert
        return Expression.Lambda<Func<TSource, object>>(conversion, param);
    }
    /// <summary>
    ///makes deleget for specific prop
    /// </summary>
    public static Func<TSource, object> GetFunc<TSource>(string propertyName)
    {
        return GetExpression<TSource>(propertyName).Compile();  //only need compiled expression
    }
    /// <summary>
    ///OrderBy overload
    /// </summary>
    public static IOrderedEnumerable<TSource>
    OrderBy<TSource>(this IEnumerable<TSource> source, string propertyName)
    {
        return source.OrderBy(GetFunc<TSource>(propertyName));
    }
    /// <summary>
    ///OrderBy overload
    /// </summary>
    public static IOrderedQueryable<TSource>
    OrderBy<TSource>(this IQueryable<TSource> source, string propertyName)
    {
        return source.OrderBy(GetExpression<TSource>(propertyName));
    }
    /// <summary>
    ///OrderByDescending overload
    /// </summary>
    public static IOrderedQueryable<TSource>
    OrderByDescending<TSource>(this IQueryable<TSource> source, string propertyName)
    {
        return source.OrderByDescending(GetExpression<TSource>(propertyName));
    }
    /// <summary>
    ///Emty predicate with returns allways true
    /// </summary>
    public static Func<TSource, bool> GetEmptyPredicate<TSource>()
    {
        ConstantExpression constant = Expression.Constant(true, typeof(bool));
        var param = Expression.Parameter(typeof(TSource), "x");
        return Expression.Lambda<Func<TSource, bool>>(constant, param).Compile();
    }
    /// <summary>
    ///Create predicate from qurey string using System.Linq.Dynamic.Core
    /// </summary>
    public static Func<TSource, bool> GetPredicate<TSource>(string expressionString)
    {
        var param = Expression.Parameter(typeof(TSource), "x");
        var e = new System.Linq.Dynamic.Core.Parser.ExpressionParser(new ParameterExpression[] { param }, expressionString, null, null);
        var parsedExpr = e.Parse(typeof(bool));
        return Expression.Lambda<Func<TSource, bool>>(parsedExpr, param).Compile();
    }
    /// <summary>
    ///Where overload
    /// </summary>
    public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, string expression)
    {
        var p = (String.IsNullOrWhiteSpace(expression)) ? GetEmptyPredicate<TSource>() : GetPredicate<TSource>(expression);
        return source.Where<TSource>(p).AsQueryable();
    }
}
