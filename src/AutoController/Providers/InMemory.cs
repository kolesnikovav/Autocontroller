using System;
using Microsoft.EntityFrameworkCore;

namespace AutoController
{
    internal static class InMemoryProvider<T> where T: DbContext, IDisposable
    {
        public static DbContextOptionsBuilder<T> GetBuilder(string connString)
        {
            var Builder = new DbContextOptionsBuilder<T>();
            Builder.UseInMemoryDatabase<T>(connString);
            return Builder;
        }
    }
}