using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text;

using System.Xml.Serialization;
using System.IO;

namespace AutoController
{
    /// <summary>
    /// Decribe access rights keys
    /// </summary>
    public class AccessKey
    {
        /// <summary>
        /// Decribe access key for user
        /// </summary>
        public IdentityUser User { get; set; }
        /// <summary>
        /// Decribe access key for role
        /// </summary>
        public IdentityRole Role { get; set; }
    }
}