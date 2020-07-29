using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoController
{
    public interface IAutoControllerOptons
    {
        string RoutePrefix {get;set;}
        string DefaultGetAction {get;set;}
        string DefaultGetCountAction {get;set;}
        string DefaultFilterParameter {get;set;}
        string DefaultSortParameter {get;set;}
        string DefaultSortDirectionParameter {get;set;}
        string DefaultPostAction {get;set;}
        DatabaseTypes DatabaseType {get;set;}
        string ConnectionString {get;set;}
        bool LogInformation {get;set;}
    }
}