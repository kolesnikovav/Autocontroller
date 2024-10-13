using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using AutoController;

namespace webapi;

[MapToController("Cats")]
[PostRestriction(Roles = "Administrator")]
public class Cats
{
    [Key]
    public Guid Id { get; set; }
    public string Nickname { get; set; }="";
    public Cats? Parent { get; set; }
}