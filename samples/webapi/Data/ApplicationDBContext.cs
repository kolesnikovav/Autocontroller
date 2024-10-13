using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace webapi;
public class ApplicationDBContext : DbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Author> Authors { get; set; }

    public DbSet<Cats> Cats { get; set; }
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
    : base(options)
    { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Blog>().Navigation(e => e.Author).AutoInclude();
        // modelBuilder.Entity<Cats>().Navigation(e => e.Parent).AutoInclude();

        Cats Tom = new Cats() {Id = Guid.NewGuid(), Nickname= "Tom"};
        Cats Jack = new Cats() {Id = Guid.NewGuid(), Nickname= "Jack"};

        modelBuilder.Entity<Cats>().HasData(
            [
                Tom,
                Jack
            ]);        
    }
}

public class ApplicationDBContextFactory : IDesignTimeDbContextFactory<ApplicationDBContext>
{
    public ApplicationDBContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDBContext>();
        optionsBuilder.UseSqlite("DataSource=app.db");
        return new ApplicationDBContext(optionsBuilder.Options);
    }
}
