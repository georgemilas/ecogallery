using Npgsql;
using System.Data;
using System.Data.Common;

namespace PicturesLib.service.database;

public interface IDatabaseService : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Execute a query and return results as a list of dynamic objects
    /// </summary>
    Task<List<Dictionary<string, object?>>> QueryAsync(string sql, object? parameters = null);
    
    /// <summary>
    /// Execute a query with a custom mapper function
    /// </summary>
    Task<List<T>> QueryAsync<T>(string sql, Func<DbDataReader, T> mapper, object? parameters = null);
    
    /// <summary>
    /// Execute a non-query command (INSERT, UPDATE, DELETE)
    /// </summary>
    Task<int> ExecuteAsync(string sql, object? parameters = null);
    
    /// <summary>
    /// Execute a scalar query (returns single value)
    /// </summary>
    Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null);
    
    /// <summary>
    /// Get a connection from the pool (remember to dispose after use)
    /// </summary>
    Task<NpgsqlConnection> GetConnectionAsync();
    
    /// <summary>
    /// Execute multiple operations within a transaction. All operations share the same connection.
    /// Automatically commits on success, rolls back on exception.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> operation);
    
    /// <summary>
    /// Execute multiple operations within a transaction without returning a value.
    /// Automatically commits on success, rolls back on exception.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<NpgsqlConnection, NpgsqlTransaction, Task> operation);
}
