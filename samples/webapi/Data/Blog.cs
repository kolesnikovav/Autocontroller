using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;

using AutoController;
using Microsoft.EntityFrameworkCore;

namespace webapi
{
    [MapToController("Blogs",InteractingType.JSON,25,false)]
    //[PostRestriction(Roles = "Administrator")]
    public class Blog :IActionBeforeSave<ApplicationDBContext>
    {
        [Key]
        [MapToControllerGetParam]
        public int Id {get;set;}
        public string Subject {get;set;}
        public string Content {get;set;}
        public bool DoBeforeSave(ApplicationDBContext context, out string reason)
        {
            reason = "";
            this.Content = this.Id.ToString() + this.Subject;
            return true;
        }
    }
}