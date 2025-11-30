using PicturesLib.model;
using PicturesLib.service;
using System.Data;

namespace PicturesLib;

/// <summary>
/// Examples demonstrating how to use the PostgreSQL database service
/// </summary>
public static class DatabaseExamples
{
    // Example model class
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Basic setup and initialization
    /// </summary>
    public static IDatabaseService CreateDatabaseService()
    {
        // Option 1: Using configuration builder
        var config = DatabaseConfiguration.CreateLocal("pictures_db", "postgres", "your_password");
        var connectionString = config.ToConnectionString();
        
        // Option 2: Direct connection string
        // var connectionString = "Host=localhost;Port=5432;Database=pictures_db;Username=postgres;Password=your_password;SSL Mode=Disable";
        
        // NpgsqlDataSource handles connection pooling - service should be long-lived (singleton/scoped)
        // Dispose when application shuts down
        return new PostgresDatabaseService(connectionString);
    }

    /// <summary>
    /// Example: Create a table
    /// </summary>
    public static async Task CreateTableExample(IDatabaseService db)
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS users (
                id SERIAL PRIMARY KEY,
                username VARCHAR(50) NOT NULL UNIQUE,
                email VARCHAR(100) NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )";
        
        await db.ExecuteAsync(sql);
        Console.WriteLine("Table created successfully");
    }

    /// <summary>
    /// Example: Insert data with parameters
    /// </summary>
    public static async Task InsertExample(IDatabaseService db)
    {
        var sql = @"
            INSERT INTO users (username, email) 
            VALUES (@Username, @Email)
            RETURNING id";
        
        var parameters = new { Username = "john_doe", Email = "john@example.com" };
        var newId = await db.ExecuteScalarAsync<int>(sql, parameters);
        
        Console.WriteLine($"Inserted user with ID: {newId}");
    }

    /// <summary>
    /// Example: Query with dynamic results
    /// </summary>
    public static async Task QueryDynamicExample(IDatabaseService db)
    {
        var sql = "SELECT * FROM users WHERE created_at > @CreatedAfter";
        var parameters = new { CreatedAfter = DateTime.UtcNow.AddDays(-7) };
        
        var results = await db.QueryAsync(sql, parameters);
        
        foreach (var row in results)
        {
            Console.WriteLine($"User: {row["username"]}, Email: {row["email"]}");
        }
    }

    /// <summary>
    /// Example: Query with strongly-typed mapper
    /// </summary>
    public static async Task QueryTypedExample(IDatabaseService db)
    {
        var sql = "SELECT id, username, email, created_at FROM users WHERE username LIKE @Pattern";
        var parameters = new { Pattern = "%john%" };
        
        var users = await db.QueryAsync(sql, reader => new User
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Username = reader.GetString(reader.GetOrdinal("username")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        }, parameters);
        
        foreach (var user in users)
        {
            Console.WriteLine($"ID: {user.Id}, Username: {user.Username}");
        }
    }

    /// <summary>
    /// Example: Update data
    /// </summary>
    public static async Task UpdateExample(IDatabaseService db)
    {
        var sql = @"
            UPDATE users 
            SET email = @Email 
            WHERE username = @Username";
        
        var parameters = new { Email = "newemail@example.com", Username = "john_doe" };
        var rowsAffected = await db.ExecuteAsync(sql, parameters);
        
        Console.WriteLine($"Updated {rowsAffected} row(s)");
    }

    /// <summary>
    /// Example: Delete data
    /// </summary>
    public static async Task DeleteExample(IDatabaseService db)
    {
        var sql = "DELETE FROM users WHERE created_at < @BeforeDate";
        var parameters = new { BeforeDate = DateTime.UtcNow.AddYears(-1) };
        
        var rowsAffected = await db.ExecuteAsync(sql, parameters);
        Console.WriteLine($"Deleted {rowsAffected} old user(s)");
    }

    /// <summary>
    /// Example: Transaction using manual connection
    /// </summary>
    public static async Task TransactionExample(IDatabaseService db)
    {
        await using var connection = await db.GetConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        
        try
        {
            // Insert user
            await using var cmd1 = connection.CreateCommand();
            cmd1.Transaction = transaction;
            cmd1.CommandText = "INSERT INTO users (username, email) VALUES (@p1, @p2) RETURNING id";
            cmd1.Parameters.AddWithValue("p1", "jane_doe");
            cmd1.Parameters.AddWithValue("p2", "jane@example.com");
            var userId = await cmd1.ExecuteScalarAsync();
            
            if (userId == null)         
            {
                throw new Exception("Failed to insert user");
            }

            // Additional operations...
            await using var cmd2 = connection.CreateCommand();
            cmd2.Transaction = transaction;
            cmd2.CommandText = "UPDATE users SET email = @p1 WHERE id = @p2";
            cmd2.Parameters.AddWithValue("p1", "updated@example.com");
            cmd2.Parameters.AddWithValue("p2", userId);
            await cmd2.ExecuteNonQueryAsync();
            
            await transaction.CommitAsync();
            Console.WriteLine("Transaction committed successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Transaction rolled back: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Example: Transaction using helper method (cleaner approach)
    /// </summary>
    public static async Task TransactionHelperExample(IDatabaseService db)
    {
        // Returns a value from the transaction
        var newUserId = await db.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            // Query 1: Insert user
            await using var cmd1 = connection.CreateCommand();
            cmd1.Transaction = transaction;
            cmd1.CommandText = "INSERT INTO users (username, email) VALUES (@p1, @p2) RETURNING id";
            cmd1.Parameters.AddWithValue("p1", "alice_smith");
            cmd1.Parameters.AddWithValue("p2", "alice@example.com");
            var userId = (int)(await cmd1.ExecuteScalarAsync() ?? 0);
            
            // Query 2: Insert related record
            await using var cmd2 = connection.CreateCommand();
            cmd2.Transaction = transaction;
            cmd2.CommandText = "INSERT INTO user_profiles (user_id, bio) VALUES (@p1, @p2)";
            cmd2.Parameters.AddWithValue("p1", userId);
            cmd2.Parameters.AddWithValue("p2", "Software developer");
            await cmd2.ExecuteNonQueryAsync();
            
            // Query 3: Update counter
            await using var cmd3 = connection.CreateCommand();
            cmd3.Transaction = transaction;
            cmd3.CommandText = "UPDATE stats SET user_count = user_count + 1";
            await cmd3.ExecuteNonQueryAsync();
            
            return userId; // All queries succeed or all rollback
        });
        
        Console.WriteLine($"Transaction completed, new user ID: {newUserId}");
        
        // Version without return value
        await db.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            await using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = "DELETE FROM users WHERE created_at < @p1";
            cmd.Parameters.AddWithValue("p1", DateTime.UtcNow.AddYears(-5));
            var deleted = await cmd.ExecuteNonQueryAsync();
            
            Console.WriteLine($"Deleted {deleted} old users in transaction");
        });
    }

    /// <summary>
    /// Example: Bulk insert
    /// </summary>
    public static async Task BulkInsertExample(IDatabaseService db)
    {
        var users = new[]
        {
            new { Username = "user1", Email = "user1@example.com" },
            new { Username = "user2", Email = "user2@example.com" },
            new { Username = "user3", Email = "user3@example.com" }
        };

        await using var connection = await db.GetConnectionAsync();
        
        foreach (var user in users)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO users (username, email) VALUES (@p1, @p2)";
            cmd.Parameters.AddWithValue("p1", user.Username);
            cmd.Parameters.AddWithValue("p2", user.Email);
            await cmd.ExecuteNonQueryAsync();
        }
        
        Console.WriteLine($"Inserted {users.Length} users");
    }

    /// <summary>
    /// Example: Get aggregate data
    /// </summary>
    public static async Task AggregateExample(IDatabaseService db)
    {
        var count = await db.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM users");
        Console.WriteLine($"Total users: {count}");
        
        var results = await db.QueryAsync(@"
            SELECT 
                DATE(created_at) as date,
                COUNT(*) as user_count
            FROM users
            GROUP BY DATE(created_at)
            ORDER BY date DESC
            LIMIT 10");
        
        foreach (var row in results)
        {
            Console.WriteLine($"Date: {row["date"]}, Count: {row["user_count"]}");
        }
    }

    /// <summary>
    /// Run all examples (for testing)
    /// </summary>
    public static async Task RunAllExamples()
    {
        // Properly dispose the service when done - this releases the connection pool
        await using var db = CreateDatabaseService();
        
        try
        {
            await CreateTableExample(db);
            await InsertExample(db);
            await QueryDynamicExample(db);
            await QueryTypedExample(db);
            await UpdateExample(db);
            await TransactionExample(db);
            await BulkInsertExample(db);
            await AggregateExample(db);
            // await DeleteExample(db); // Commented to preserve test data
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        // db.DisposeAsync() called automatically here
    }
}
