using AutoController;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DbContextExample;

[MapToController("Posts", InteractingType.JSON, 25, false)]
public class Post
{
    [Key]
    public int Id { get; set; }
    public string Content { get; set; }
}

