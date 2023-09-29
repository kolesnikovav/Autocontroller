using Microsoft.EntityFrameworkCore;

namespace AutoController;

/// <summary>
/// Implement this interface in your entity types
/// </summary>
public interface IActionBeforeDelete<T> where T : DbContext
{
    /// <summary>
    /// Do something before delete
    /// </summary>
    /// <param name="dbcontext">DbContext</param>
    /// <param name="reason">The reason why the object cannot be removed</param>
    public bool DoBeforeDelete(T dbcontext, out string reason);
}
