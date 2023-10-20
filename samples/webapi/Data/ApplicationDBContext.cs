using Microsoft.EntityFrameworkCore;

public class ApplicationDBContext : DbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
    : base(options)
    { }
}
