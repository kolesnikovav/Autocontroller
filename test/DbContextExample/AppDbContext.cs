using Microsoft.EntityFrameworkCore;

namespace DbContextExample;
    public class AppDBContext: DbContext
    {
        public DbSet<Blog> Blogs {get;set;}
        public DbSet<Post> Posts {get;set;}
        public AppDBContext(DbContextOptions<AppDBContext> options)
        :base(options)
        {}
    }