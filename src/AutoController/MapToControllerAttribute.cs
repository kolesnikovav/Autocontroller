﻿using System;

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
        /// DBSet<T> property name for access elements collection
        /// </summary>
        public string CollectionName { get; set; }
        /// <summary>
        /// Discribe interacting method for controller
        /// </summary>
        public InteractingType InteractingType { get; set; }
        /// <summary>
        /// Items per page, 0 by default
        /// </summary>
        public uint ItemsPerPage { get; set; } = 0;
        /// <summary>
        /// The name of default get action
        /// Index by default
        /// </summary>
        public string DefaultGetAction { get; set; }
        /// <summary>
        /// Constructor with specified controller name
        /// </summary>
        public MapToControllerAttribute(string controllerName, string collectionName, InteractingType iType, uint itemsPerPage = 0, string defaultGetAction = "Index")
        {
            ControllerName = controllerName;
            CollectionName = collectionName;
            InteractingType = iType;
            ItemsPerPage = itemsPerPage;
            DefaultGetAction = defaultGetAction;
        }
    }
}