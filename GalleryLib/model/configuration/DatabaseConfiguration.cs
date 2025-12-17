namespace GalleryLib.model.configuration;

public class DatabaseConfiguration
{
    public const string SectionName = "Database";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int MaxPoolSize { get; set; } = 100;
    public int MinPoolSize { get; set; } = 0;
    public int ConnectionLifetime { get; set; } = 300; // seconds = 5 minutes
    public int CommandTimeout { get; set; } = 30; // seconds
    public bool Pooling { get; set; } = true;
    public string SslMode { get; set; } = "Prefer"; // Disable, Allow, Prefer, Require

    /// <summary>
    /// Builds a PostgreSQL connection string from the configuration properties
    /// </summary>
    public string ToConnectionString()
    {
        return $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};" +
               $"Pooling={Pooling};MinPoolSize={MinPoolSize};MaxPoolSize={MaxPoolSize};" +
               $"Connection Lifetime={ConnectionLifetime};Command Timeout={CommandTimeout};SSL Mode={SslMode};" +
               $"Include Error Detail=true";
    }

    /// <summary>
    /// Creates a configuration for local development with default settings
    /// </summary>
    public static DatabaseConfiguration CreateLocal(string database, string username = "postgres", string password = "postgres")
    {
        return new DatabaseConfiguration
        {
            Host = "localhost",
            Port = 5432,
            Database = database,
            Username = username,
            Password = password,
            SslMode = "Disable"
        };
    }
}
