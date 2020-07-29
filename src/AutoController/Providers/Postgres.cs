using System;
using Microsoft.EntityFrameworkCore;

namespace AutoController
{
    internal static class PostgresProvider<T> where T: DbContext, IDisposable
    {
        public static DbContextOptionsBuilder<T> GetBuilder(string connString)
        {
            var Builder = new DbContextOptionsBuilder<T>();
            Builder.UseNpgsql(connString);
            return Builder;
        }
    }
}