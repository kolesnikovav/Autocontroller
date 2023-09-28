using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AutoController;
using Microsoft.EntityFrameworkCore;

namespace webapi;
public class ApplicationDBContext : DbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
    : base(options)
    { }
}
