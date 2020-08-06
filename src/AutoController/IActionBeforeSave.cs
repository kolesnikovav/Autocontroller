using Microsoft.EntityFrameworkCore;

namespace AutoController
{
    /// <summary>
    /// Implement this interface in your entity types
    /// </summary>
    public interface IActionBeforeSave<T> where T:DbContext
    {
        /// <summary>
        /// Do something before save
        /// </summary>
        /// <param name="dbcontext">DbContext</param>
        /// <param name="reason">The reason why the object cannot be saved</param>
        public bool DoBeforeSave(T dbcontext, out string reason);
    }
}