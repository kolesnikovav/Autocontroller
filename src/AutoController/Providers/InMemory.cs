using System;
using Microsoft.EntityFrameworkCore;

namespace AutoController;

internal static class InMemoryProvider<T> where T : DbContext, IDisposable
{
    public static DbContextOptionsBuilder<T> GetBuilder(string connString)
    {
        var Builder = new DbContextOptionsBuilder<T>();
        Builder.UseInMemoryDatabase<T>(connString);
        return Builder;
    }
    public static DbContextOptionsBuilder<T> GetBuilder(string connString, DbContextOptions<T> options)
    {
        var Builder = new DbContextOptionsBuilder<T>(options);
        Builder.UseInMemoryDatabase(connString);
        return Builder;
    }    
}
