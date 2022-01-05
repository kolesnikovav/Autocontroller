using AutoController;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;

namespace DbContextExample
{
    [MapToController("Blogs", InteractingType.JSON, 25, false)]
    public class Blog
    {
        [Key]
        public int Id { get; set; }
        public string Description { get; set; }
    }
}
