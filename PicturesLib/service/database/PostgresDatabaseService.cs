using Npgsql;
using System.Data;
using System.Data.Common;

namespace PicturesLib.service.database;

public class PostgresDatabaseService : IDatabaseService
{
    private readonly NpgsqlDataSource _dataSource;
    private bool _disposed;

    /// <summary>
    /// Creates a database service with connection pooling using NpgsqlDataSource
    /// </summary>
    public PostgresDatabaseService(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException(nameof(connectionString));

        // NpgsqlDataSource manages connection pooling automatically
        _dataSource = NpgsqlDataSource.Create(connectionString);
    }

    /// <summary>
    /// Get a pooled connection - caller must dispose
    /// </summary>
    public async Task<NpgsqlConnection> GetConnectionAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return await _dataSource.OpenConnectionAsync();
    }

    public async Task<List<Dictionary<string, object?>>> QueryAsync(string sql, object? parameters = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        await using var connection = await GetConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        
        AddParameters(command, parameters);
        
        var results = new List<Dictionary<string, object?>>();
        await using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            results.Add(row);
        }
        
        return results;
    }

    public async Task<List<T>> QueryAsync<T>(string sql, Func<DbDataReader, T> mapper, object? parameters = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        await using var connection = await GetConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        
        AddParameters(command, parameters);
        
        var results = new List<T>();
        await using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            results.Add(mapper(reader));
        }
        
        return results;
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        await using var connection = await GetConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        
        AddParameters(command, parameters);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        await using var connection = await GetConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        
        AddParameters(command, parameters);
        
        var result = await command.ExecuteScalarAsync();
        
        if (result == null || result == DBNull.Value)
            return default;
        
        return (T)result;
    }

    private static void AddParameters(NpgsqlCommand command, object? parameters)
    {
        if (parameters == null)
            return;

        var properties = parameters.GetType().GetProperties();
        foreach (var prop in properties)
        {
            var value = prop.GetValue(parameters);
            command.Parameters.AddWithValue($"@{prop.Name}", value ?? DBNull.Value);
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> operation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        await using var connection = await GetConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        
        try
        {
            var result = await operation(connection, transaction);
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ExecuteInTransactionAsync(Func<NpgsqlConnection, NpgsqlTransaction, Task> operation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        await using var connection = await GetConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        
        try
        {
            await operation(connection, transaction);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _dataSource.Dispose();
            }
            _disposed = true;
        }
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            await _dataSource.DisposeAsync().ConfigureAwait(false);
            _disposed = true;
        }
    }
}
