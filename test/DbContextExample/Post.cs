using AutoController;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;

namespace DbContextExample
{
    [MapToController("Posts", InteractingType.JSON, 25, false)]
    public class Post
    {
        public Blog Blog { get; set; }
        public int Id { get; set; }
        public string Content { get; set; }
    }
}
