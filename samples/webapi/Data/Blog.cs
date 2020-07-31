using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;

using AutoController;
using Microsoft.EntityFrameworkCore;

namespace webapi
{
    [MapToController("Blogs",InteractingType.JSON,25,false)]
    [PostRestiction(Roles = "Administrator")]
    public class Blog
    {
        [Key]
        [MapToControllerGetParam]
        public int Id {get;set;}
        public string Subject {get;set;}
        public string Content {get;set;}
    }
}