using GalleryLib.model.configuration;
using GalleryLib.Repository.Auth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GalleryLib.service.database;

public class CreateDatabaseService
{
    private readonly DatabaseConfiguration _dbConfig;
    private readonly IDatabaseService _databaseService;

    public CreateDatabaseService(DatabaseConfiguration dbConfig)
    {
        _dbConfig = dbConfig;
        // Connect to 'postgres' database instead of target database to avoid "cannot drop currently open database" error
        var connectionString = GetSystemDatabaseConnectionString(dbConfig);
        _databaseService = new PostgresDatabaseService(connectionString);
    }

    public async Task<bool> CreateDatabaseAsync(string? databaseName = null, string password = "admin123")
    {
        try
        {
            // Find the solution root by looking for .sln file
            var dbFolderPath = FindDatabaseFolder();
            if (dbFolderPath == null)
            {
                Console.WriteLine("ERROR: Database folder not found: GalleryLib/db");
                return false;
            }

            // Use provided database name or fall back to config
            var targetDatabaseName = databaseName ?? _dbConfig.Database;
            if (string.IsNullOrWhiteSpace(targetDatabaseName))
            {
                Console.WriteLine("ERROR: No database name provided and none configured");
                return false;
            }
            
            Console.WriteLine($"Using database folder: {dbFolderPath}");
            Console.WriteLine($"Target database: {targetDatabaseName}");

            var allSqlFiles = Directory.GetFiles(dbFolderPath, "*.sql").Select(Path.GetFileName).Where(f => f != null).Cast<string>().ToList();
            if (!allSqlFiles.Any())
            {
                Console.WriteLine("ERROR: No SQL files found in database folder");
                return false;
            }

            // Define execution order - create_db.sql first, clear_database.sql, db.sql, then others
            var executionOrder = new List<string>();
            executionOrder.Add("create_db.sql");
            executionOrder.Add("clear_database.sql");
            executionOrder.Add("db.sql");
            // All other files except the static files
            var staticFiles = new[] { "create_db.sql", "clear_database.sql", "db.sql", "create_admin_user.sql" };
            var middleFiles = allSqlFiles
                .Where(f => !staticFiles.Contains(f))
                .OrderBy(f => f)
                .ToList();
            executionOrder.AddRange(middleFiles);
            executionOrder.Add("create_admin_user.sql");

            Console.WriteLine($"Total files to execute: {executionOrder.Count}");
            Console.WriteLine();
                        
            // Execute files in order with connection switching
            IDatabaseService? targetDbService = null;
            
            for (int i = 0; i < executionOrder.Count; i++)
            {
                var fileName = executionOrder[i];
                var filePath = Path.Combine(dbFolderPath, fileName);
                
                IDatabaseService dbServiceToUse;
                if (fileName == "create_db.sql")
                {
                    // Use postgres connection for database creation
                    dbServiceToUse = _databaseService;
                    Console.WriteLine("Using postgres connection for database creation");
                }
                else
                {
                    // Switch to target database connection after create_db.sql
                    if (targetDbService == null)
                    {
                        Console.WriteLine($"Switching to target database connection: {targetDatabaseName}");
                        var targetDbConfig = new DatabaseConfiguration 
                        {
                            Host = _dbConfig.Host,
                            Port = _dbConfig.Port,
                            Database = targetDatabaseName,
                            Username = _dbConfig.Username,
                            Password = _dbConfig.Password,
                            Pooling = _dbConfig.Pooling,
                            MinPoolSize = _dbConfig.MinPoolSize,
                            MaxPoolSize = _dbConfig.MaxPoolSize,
                            ConnectionLifetime = _dbConfig.ConnectionLifetime,
                            CommandTimeout = _dbConfig.CommandTimeout,
                            SslMode = _dbConfig.SslMode
                        };
                        targetDbService = new PostgresDatabaseService(targetDbConfig.ToConnectionString());
                    }
                    dbServiceToUse = targetDbService;
                }
                                
                if (await ExecuteSqlFileAsync(filePath, targetDatabaseName, dbServiceToUse))
                {
                    if (fileName == "create_admin_user.sql")
                    {
                        Console.WriteLine("Admin user creation completed (LAST)");
                        var passwordHash = AuthRepository.HashPassword(password);
                        const string sql = "UPDATE public.users SET password_hash = @password_hash WHERE username = 'admin'";
                        await dbServiceToUse.ExecuteAsync(sql, new { password_hash = passwordHash });                        
                    }
                    else if (fileName == "create_db.sql")
                    {
                        Console.WriteLine("Database creation completed (FIRST)");
                    }
                    else if (fileName == "clear_database.sql")
                    {
                        Console.WriteLine("Database cleared for fresh schema");
                    }
                    else if (fileName == "db.sql")
                    {
                        Console.WriteLine("Main schema creation completed");
                    }
                    else
                    {
                        Console.WriteLine($"{fileName} completed");
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to execute: {fileName}");
                    targetDbService?.Dispose();
                    return false;
                }
            }
            
            // Clean up target database service
            targetDbService?.Dispose();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database creation failed: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteSqlFileAsync(string filePath, string targetDatabaseName, IDatabaseService dbService)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"ERROR: SQL file not found: {filePath}");
                return false;
            }

            var sqlContent = await File.ReadAllTextAsync(filePath);
            
            if (string.IsNullOrWhiteSpace(sqlContent))
            {
                Console.WriteLine($"Warning: {Path.GetFileName(filePath)} is empty");
                return true; // Consider empty files as successful
            }

            // Replace hardcoded database name with target database name for create_db.sql
            if (Path.GetFileName(filePath).Equals("create_db.sql", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Replacing database references with '{targetDatabaseName}' in {Path.GetFileName(filePath)}");
                // Replace database name patterns (case insensitive)
                sqlContent = System.Text.RegularExpressions.Regex.Replace(
                    sqlContent, 
                    @"\b(Drop database if exists|Create Database)\s+\w+\b", 
                    match => match.Groups[1].Value + " " + targetDatabaseName,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
            }

            // Split SQL content into statements and execute each one
            var statements = sqlContent
                .Split(new[] { ";\r\n", ";\n", ";" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            foreach (var statement in statements)
            {
                try
                {
                    await dbService.ExecuteAsync(statement);
                }
                catch (Exception ex)
                {
                    // Some statements might fail if objects already exist - log but continue
                    Console.WriteLine($"Warning in {Path.GetFileName(filePath)}: {ex.Message}");
                    Console.WriteLine($"   Statement: {statement.Substring(0, Math.Min(100, statement.Length))}...");
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Error executing {Path.GetFileName(filePath)}: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Find the database folder by searching up the directory tree for the solution root
    /// </summary>
    private static string? FindDatabaseFolder()
    {
        // Start from the current application directory
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        
        // Search up the directory tree for the solution root (containing .sln file)
        while (currentDir != null)
        {
            // Look for .sln file to identify solution root
            if (currentDir.GetFiles("*.sln").Any())
            {
                var dbPath = Path.Combine(currentDir.FullName, "GalleryLib", "db");
                if (Directory.Exists(dbPath))
                {
                    return dbPath;
                }
            }
            currentDir = currentDir.Parent;
        }
        
        // Fallback: try relative to current working directory
        var fallbackPath = Path.Combine(Directory.GetCurrentDirectory(), "GalleryLib", "db");
        if (Directory.Exists(fallbackPath))
        {
            return fallbackPath;
        }
        
        // Final fallback: try relative to GalleryLib project (when running from GalleryService)
        var galleryLibPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "GalleryLib", "db");
        if (Directory.Exists(galleryLibPath))
        {
            return Path.GetFullPath(galleryLibPath);
        }
        
        return null;
    }

    /// <summary>
    /// Create a connection string that connects to the 'postgres' system database
    /// instead of the target database to avoid "cannot drop currently open database" errors
    /// </summary>
    private static string GetSystemDatabaseConnectionString(DatabaseConfiguration dbConfig)
    {
        return $"Host={dbConfig.Host};Port={dbConfig.Port};Database=postgres;Username={dbConfig.Username};Password={dbConfig.Password};" +
               $"Pooling={dbConfig.Pooling};MinPoolSize={dbConfig.MinPoolSize};MaxPoolSize={dbConfig.MaxPoolSize};" +
               $"Connection Lifetime={dbConfig.ConnectionLifetime};Command Timeout={dbConfig.CommandTimeout};SSL Mode={dbConfig.SslMode};" +
               $"Include Error Detail=true";
    }
}