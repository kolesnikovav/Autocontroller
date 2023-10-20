using System.ComponentModel.DataAnnotations;

using AutoController;

namespace webapi;

[MapToController("Blogs", InteractingType.JSON, 25, false)]
// [GetRestriction(Roles = "Administrator")]
[PostRestriction(Roles = "Administrator")]
public class Blog : IActionBeforeSave<ApplicationDBContext>
{
    [Key]
    [MapToControllerGetParam]
    public int Id { get; set; }
    public string Subject { get; set; }
    public string Content { get; set; }
    public bool DoBeforeSave(ApplicationDBContext context, out string reason)
    {
        reason = "";
        this.Content = this.Id.ToString() + this.Subject;
        return true;
    }
}
