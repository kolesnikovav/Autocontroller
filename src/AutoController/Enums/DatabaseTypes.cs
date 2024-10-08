namespace AutoController;

/// <summary>
/// Decribe database type for your DBContext
/// </summary>
public enum DatabaseTypes
{
    /// <summary>
    /// SQLite database
    /// </summary>
    SQLite,
    /// <summary>
    /// SQLServer database
    /// </summary>
    SQLServer,
    /// <summary>
    /// PostgreSQL database
    /// </summary>
    Postgres,
    /// <summary>
    /// MySQL database (by Oracle)
    /// </summary>
    MySQL,
    /// <summary>
    /// InMemory database (for testing only)
    /// </summary>
    InMemory

}
