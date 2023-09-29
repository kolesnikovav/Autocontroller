using System;

namespace AutoController;

/// <summary>
/// Map target class to specified controller
/// By default, Controller name is the same as class name
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class MapToControllerAttribute : Attribute
{
    /// <summary>
    /// Map target class to specified controller with name
    /// </summary>
    public string ControllerName { get; set; }
    /// <summary>
    /// Discribe interacting method for controller
    /// </summary>
    public InteractingType InteractingType { get; set; }
    /// <summary>
    /// Items per page, 0 by default
    /// </summary>
    public uint ItemsPerPage { get; set; } = 0;
    /// <summary>
    /// Allow anauthorized access for obtaining data
    /// </summary>
    public bool AllowAnonimus { get; set; } = true;
    /// <summary>
    /// Constructor with specified controller name
    /// </summary>
    public MapToControllerAttribute(string controllerName, InteractingType iType, uint itemsPerPage = 0, bool allowAnonimus = true)
    {
        ControllerName = controllerName;
        InteractingType = iType;
        ItemsPerPage = itemsPerPage;
        AllowAnonimus = allowAnonimus;
    }
}
