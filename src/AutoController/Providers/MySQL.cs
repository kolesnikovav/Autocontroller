using System;
using Microsoft.EntityFrameworkCore;

namespace AutoController
{
    internal static class MySQLProvider<T> where T: DbContext, IDisposable
    {
        public static DbContextOptionsBuilder<T> GetBuilder(string connString)
        {
            var Builder = new DbContextOptionsBuilder<T>();
            Builder.UseMySQL(connString);
            return Builder;
        }
    }
}