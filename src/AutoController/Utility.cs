// Getting from https://www.codeproject.com/Tips/817372/Building-OrderBy-Lambda-Expression-from-Property-N
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AutoController
{
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
        public static Func<TSource, bool> GetPredicate<TSource>(string propertyName)
        {
            var signs = new string[] {"=", "<=", ">=", "<", ">", "<>", "!="};
            var param = Expression.Parameter(typeof(TSource), "x");
            Expression conversion = Expression.Convert(Expression.Property
            (param, propertyName), typeof(object));   //important to use the Expression.Convert
            return Expression.Lambda<Func<TSource, bool>>(conversion, param).Compile();
        }
        /// <summary>
        ///Where overload
        /// </summary>
        public static IEnumerable<TSource> Where<TSource> (this IEnumerable<TSource> source, string expression)
        {
            return source.Where<TSource>(GetPredicate<TSource>(expression));
        }
    }
}