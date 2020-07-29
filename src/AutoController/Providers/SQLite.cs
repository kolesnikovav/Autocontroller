using System;
using Microsoft.EntityFrameworkCore;

namespace AutoController
{
    internal static class SQLiteProvider<T> where T: DbContext, IDisposable
    {
        public static DbContextOptionsBuilder<T> GetBuilder(string connString)
        {
            var Builder = new DbContextOptionsBuilder<T>();
            Builder.UseSqlite(connString);
            return Builder;
        }
    }
}