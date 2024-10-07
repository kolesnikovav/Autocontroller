using Microsoft.EntityFrameworkCore;

namespace webapi;
public class ApplicationDBContext : DbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Author> Authors { get; set; }
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
    : base(options)
    { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Blog>().Navigation(e => e.Author).AutoInclude();

    }    
}
