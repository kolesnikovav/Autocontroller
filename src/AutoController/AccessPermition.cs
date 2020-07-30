using System;

namespace AutoController
{
    /// <summary>
    /// Decribe access permition for Entity/Field
    /// </summary>
    public enum AccessPermition
    {
        /// <summary>
        /// Read access allowed
        /// </summary>
        Read,
        /// <summary>
        /// Write/Read access allowed
        /// </summary>
        Write,
        /// <summary>
        /// Access not allowed
        /// </summary>
        None
    }
}