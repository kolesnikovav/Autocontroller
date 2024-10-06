using System.ComponentModel.DataAnnotations;

using AutoController;

namespace webapi;

[MapToController("Authors", InteractingType.JSON, 25, false)]
// [GetRestriction(Roles = "Administrator")]
[PostRestriction(Roles = "Administrator")]
public class Author
{
    [Key]
    [MapToControllerGetParam]
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}