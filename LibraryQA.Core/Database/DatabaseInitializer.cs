using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace LibraryQA.Core.Database
{
    /// Handles database initialization and schema creation for the Library Management System.
    /// This class reads and executes the DatabaseSchema.sql script to create all tables,
    /// indexes, and constraints in the SQLite database.
    public class DatabaseInitializer
    {
        private readonly string _connectionString;
        private readonly string _databasePath;

        
        /// Initializes a new instance of DatabaseInitializer with the specified database path.
        public DatabaseInitializer(string databasePath)
        {
            _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
            _connectionString = $"Data Source={databasePath}";
        }

        
        /// Creates the database file and initializes the schema by executing DatabaseSchema.sql.
        /// If the database already exists, it will be dropped and recreated.
        public bool InitializeDatabase()
        {
            try
            {
                // Ensure the directory exists
                string? directory = Path.GetDirectoryName(_databasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create or overwrite the database file by opening a connection
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    // Enable foreign key constraints (SQLite has them disabled by default)
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "PRAGMA foreign_keys = ON;";
                        command.ExecuteNonQuery();
                    }

                    // Read and execute the schema SQL script
                    string schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "DatabaseSchema.sql");

                    if (!File.Exists(schemaPath))
                    {
                        throw new FileNotFoundException($"Database schema file not found at: {schemaPath}");
                    }

                    string schemaSql = File.ReadAllText(schemaPath);

                    // Execute the schema script
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = schemaSql;
                        command.ExecuteNonQuery();
                    }
                }

                Console.WriteLine($"Database initialized successfully at: {_databasePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing database: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        
        /// Checks if the database file exists and contains the required tables.
        public bool DatabaseExists()
        {
            if (!File.Exists(_databasePath))
            {
                return false;
            }

            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    // Check if core tables exist
                    string[] expectedTables = { "Accounts", "Books", "Loans", "Reservations" };

                    using (var command = connection.CreateCommand())
                    {
                        foreach (string table in expectedTables)
                        {
                            command.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{table}';";
                            var result = command.ExecuteScalar();

                            if (result == null || result.ToString() != table)
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        
        /// Drops and recreates the database with fresh schema.
        /// WARNING: This will delete all existing data.
        public bool ResetDatabase()
        {
            try
            {
                // Delete the database file if it exists
                if (File.Exists(_databasePath))
                {
                    File.Delete(_databasePath);
                    Console.WriteLine($"Existing database deleted: {_databasePath}");
                }

                // Reinitialize with clean schema
                return InitializeDatabase();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting database: {ex.Message}");
                return false;
            }
        }

        
        /// Gets the connection string for the configured database.
        public string ConnectionString => _connectionString;

        /// Gets the full path to the database file.
        public string DatabasePath => _databasePath;
    }
}
