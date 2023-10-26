using System;
using Microsoft.EntityFrameworkCore;

namespace AutoController;

internal static class SQLServerProvider<T> where T : DbContext, IDisposable
{
    public static DbContextOptionsBuilder<T> GetBuilder(string connString)
    {
        var Builder = new DbContextOptionsBuilder<T>();
        Builder.UseSqlServer(connString);
        return Builder;
    }
    public static DbContextOptionsBuilder<T> GetBuilder(string connString, DbContextOptions<T> options)
    {
        var Builder = new DbContextOptionsBuilder<T>(options);
        Builder.UseSqlServer(connString);
        return Builder;
    }    

}
