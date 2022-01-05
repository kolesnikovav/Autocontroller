using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DbContextExample
{
    public class AppDBContext : DbContext
    {
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }
        public AppDBContext(DbContextOptions<AppDBContext> options)
        : base(options)
        { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }
}
