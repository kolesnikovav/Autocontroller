using Microsoft.EntityFrameworkCore;

namespace webapi;
public class ApplicationDBContext : DbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
    : base(options)
    { }
}
