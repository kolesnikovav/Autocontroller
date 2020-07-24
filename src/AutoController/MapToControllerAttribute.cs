using System;

namespace AutoController
{
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
        /// Default constructor
        /// </summary>
        public MapToControllerAttribute()
        {

        }
        /// <summary>
        /// Constructor with specified controller name
        /// </summary>
        public MapToControllerAttribute(string controllerName)
        {
            ControllerName = controllerName;
        }
    }
}
