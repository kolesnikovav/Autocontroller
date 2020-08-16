using System;

namespace AutoController
{
    /// <summary>
    /// Map target property to specified controller param for get request
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class MapToControllerGetParamAttribute : Attribute
    {
        /// <summary>
        /// Map target property to specified controller param with name
        /// </summary>
        public string ParamName { get; set; }
        /// <summary>
        /// Determine that parameter is optional (can be ommited during request)
        /// True by default
        /// </summary>
        public bool Optional { get; set; }
        /// <summary>
        /// Default constructor
        /// </summary>
        public MapToControllerGetParamAttribute()
        {
            Optional = true;
        }
        /// <summary>
        /// Constructor with specified controller param name
        /// </summary>
        public MapToControllerGetParamAttribute(string paramName, bool optional)
        {
            ParamName = paramName;
            Optional = optional;
        }
        /// <summary>
        /// Constructor with specified controller param name
        /// </summary>
        public MapToControllerGetParamAttribute(string paramName)
        {
            ParamName = paramName;
        }
        /// <summary>
        /// Constructor with specified controller param is optional
        /// </summary>
        public MapToControllerGetParamAttribute(bool optional)
        {
            Optional = optional;
        }
    }
}
